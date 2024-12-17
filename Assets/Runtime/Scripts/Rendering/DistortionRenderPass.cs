using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace TinyTanks.Rendering
{
    public class DistortionRenderPass : ScriptableRenderPass
    {
        public Material blitMaterial;

        private static List<ShaderTagId> shaderTagList = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        public DistortionRenderPass() { profilingSampler = new ProfilingSampler("Distortion Render Pass"); }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (blitMaterial == null) return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var textureDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
            textureDesc.name = "_DistortionColorBuffer";
            var intermediateTexture = renderGraph.CreateTexture(textureDesc);

            textureDesc.name = "_DistortionTexture";
            textureDesc.colorFormat = GraphicsFormat.R16G16_SFloat;
            var distortionTexture = renderGraph.CreateTexture(textureDesc);

            using (var builder = renderGraph.AddRasterRenderPass<RenderPassData>("Render Distortion", out var data, profilingSampler))
            {
                var cullResults = renderingData.cullResults;
                var drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagList, renderingData, cameraData, lightData, SortingCriteria.None);
                var filterSettings = new FilteringSettings(RenderQueueRange.all, 0b1 << 9);
                data.renderList = renderGraph.CreateRendererList(new RendererListParams(cullResults, drawingSettings, filterSettings));

                data.distortionTexture = distortionTexture;

                builder.UseRendererList(data.renderList);
                builder.SetRenderAttachment(data.distortionTexture, 0);

                builder.SetRenderFunc((RenderPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    using (new ProfilingScope(cmd, profilingSampler))
                    {
                        cmd.ClearRenderTarget(true, false, clearColor);
                        cmd.DrawRendererList(data.renderList);
                    }
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<RenderPassData>("Create Color Attachment Copy", out var data, profilingSampler))
            {
                data.intermediateTexture = intermediateTexture;
                data.cameraColorTexture = resourceData.activeColorTexture;
            
                builder.UseTexture(data.cameraColorTexture);
                builder.SetRenderAttachment(data.intermediateTexture, 0);
            
                builder.SetRenderFunc((RenderPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    using (new ProfilingScope(cmd, profilingSampler))
                    {
                        Blitter.BlitTexture(cmd, data.cameraColorTexture, new Vector4(1f, 1f, 0f, 0f), 0f, true);
                    }
                });
            }
            
            using (var builder = renderGraph.AddRasterRenderPass<RenderPassData>("Apply Distortion Post Process", out var data, profilingSampler))
            {
                data.distortionTexture = distortionTexture;
                data.intermediateTexture = intermediateTexture;
                
                builder.UseTexture(data.intermediateTexture);
                builder.UseTexture(data.distortionTexture);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((RenderPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    using (new ProfilingScope(cmd, profilingSampler))
                    {
                        cmd.SetGlobalTexture("_DistortionColorBuffer", data.intermediateTexture);
                        Blitter.BlitTexture(cmd, data.distortionTexture, new Vector4(1f, 1f, 0f, 0f), blitMaterial, 0);
                    }
                });
            }
        }

        public class RenderPassData
        {
            public RendererListHandle renderList;
            public TextureHandle cameraColorTexture;
            public TextureHandle intermediateTexture;
            public TextureHandle distortionTexture;
        }
    }
}