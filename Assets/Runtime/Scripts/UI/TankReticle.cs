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

        private Vector2 offset;
        private Vector2 smoothedOffset;
        
        private TankController tank;
        private Camera mainCamera;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(true);
        }

        private void OnDisable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!tank.isActiveViewer) return;

            offset = mainCamera.WorldToScreenPoint(tank.simModel.gunPivot.position + tank.simModel.gunPivot.forward * tank.targetingRange);
            smoothedOffset = Vector2.Lerp(smoothedOffset, offset, Time.deltaTime / Mathf.Max(Time.deltaTime, offsetSmoothing));
            transform.position = new Vector3(smoothedOffset.x, smoothedOffset.y, transform.position.z);
            scopeFeature.passMaterial.SetVector("_Offset", smoothedOffset - new Vector2(Screen.width, Screen.height) / 2f);
        }
    }
}