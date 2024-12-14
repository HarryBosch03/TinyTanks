using UnityEngine.Rendering.Universal;

namespace TinyTanks.Rendering
{
    public class CdmRendererFeature : ScriptableRendererFeature
    {
        public CdmRenderPass pass;

        public bool renderInScene;
        
        public override void Create()
        {
            pass = new CdmRenderPass();
            pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            pass.renderInScene = renderInScene;
            renderer.EnqueuePass(pass);
        }
    }
}