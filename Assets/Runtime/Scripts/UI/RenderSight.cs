using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TinyTanks.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class RenderSight : MonoBehaviour
    {
        public ScriptableRendererFeature scopeFeature;

        private void OnEnable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(true);
        }

        private void OnDisable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(false);
        }
    }
}