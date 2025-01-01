using TinyTanks.Tanks;
using UnityEngine;

namespace TinyTanks.UI
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(RectTransform))]
    public class TankReticle : MonoBehaviour
    {
        public FullScreenPassRendererFeature scopeFeature;
        public float offsetSmoothing = 0.1f;
        public float lagAmplitude;

        private Vector2 offset;
        private Vector2 smoothedOffset;

        private TankController tank;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
        }

        private void OnEnable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(true);
            offset = GetOffset();
            smoothedOffset = offset;
        }

        private void OnDisable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!tank.isActiveViewer) return;
            var transform = this.transform as RectTransform;

            offset = GetOffset();
            smoothedOffset = Vector2.Lerp(smoothedOffset, offset, Time.deltaTime / Mathf.Max(Time.deltaTime, offsetSmoothing));
            transform.anchoredPosition = new Vector2(smoothedOffset.x, smoothedOffset.y);
            Shader.SetGlobalVector("_ScopeOffset", smoothedOffset);

            transform.localScale = Vector3.one * tank.sightZoom;
        }

        private Vector2 GetOffset() => -tank.turretVelocity * lagAmplitude;

        private void OnValidate()
        {
            var transform = this.transform as RectTransform;
        }
    }
}