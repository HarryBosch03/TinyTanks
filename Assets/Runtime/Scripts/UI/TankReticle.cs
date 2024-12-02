using System;
using TinyTanks.Tanks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TinyTanks.UI
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(RectTransform))]
    public class TankReticle : MonoBehaviour
    {
        public ScriptableRendererFeature scopeFeature;
        public float offsetSmoothing = 0.1f;

        private float offset;
        private float smoothedOffset;
        
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
            smoothedOffset = transform.position.y;
        }

        private void OnDisable()
        {
            if (scopeFeature != null) scopeFeature.SetActive(false);
        }

        private void FixedUpdate()
        {
            offset = mainCamera.WorldToScreenPoint(tank.sightAimPoint).y;
        }

        private void LateUpdate()
        {
            smoothedOffset = Mathf.Lerp(smoothedOffset, offset, Time.deltaTime / Mathf.Max(Time.deltaTime, offsetSmoothing));
            transform.position = new Vector3(transform.position.x, smoothedOffset, transform.position.z);
        }
    }
}