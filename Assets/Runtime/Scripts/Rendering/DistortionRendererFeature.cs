using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TinyTanks.Rendering
{
    public class DistortionRendererFeature : ScriptableRendererFeature
    {
        public DistortionRenderPass pass;
        public float intensity = 1f;
        public Material blitMaterial;

        public override void Create()
        {
            pass = new DistortionRenderPass();
            pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            pass.blitMaterial = blitMaterial;
            blitMaterial.SetFloat("_Intensity", intensity);
            renderer.EnqueuePass(pass);
        }
    }
}