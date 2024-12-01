using UnityEngine.Rendering.Universal;

namespace TinyTanks.Rendering
{
    public class CdmRendererFeature : ScriptableRendererFeature
    {
        public CdmRenderPass pass;
        
        public override void Create()
        {
            pass = new CdmRenderPass();
            pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
    }
}