using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace TinyTanks.Rendering
{
    public class CdmRenderPass : ScriptableRenderPass
    {
        private static List<ShaderTagId> shaderTagList = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        public CdmRenderPass() { profilingSampler = new ProfilingSampler("Cdm Render Pass"); }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            using (var builder = renderGraph.AddRasterRenderPass<RenderPassData>("Cdm Render Pass", out var data, profilingSampler))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                var cullResults = renderingData.cullResults;
                var drawingSettings = RenderingUtils.CreateDrawingSettings(shaderTagList, renderingData, cameraData, lightData, SortingCriteria.None);
                var filterSettings = new FilteringSettings(RenderQueueRange.all, 0b1 << 8);
                data.renderList = renderGraph.CreateRendererList(new RendererListParams(cullResults, drawingSettings, filterSettings));
                builder.UseRendererList(data.renderList);

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                
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
        }

        public class RenderPassData
        {
            public RendererListHandle renderList;
        }
    }
}