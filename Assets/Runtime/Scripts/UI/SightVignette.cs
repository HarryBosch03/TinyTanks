using System;
using TinyTanks.Tanks;
using UnityEngine;

namespace TinyTanks.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SightVignette : MonoBehaviour
    {
        public float texturePad;
        public float lagSpring;
        public float lagDamping;
        public float lagSlope = 1f;
        public float lagMax;

        private Canvas canvas;
        private TankController tank;
        private RectTransform rectTransform => transform as RectTransform;

        private Vector2 lagPosition;
        private Vector2 lagVelocity;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            lagPosition = tank.turretRotation;
            lagVelocity = Vector3.zero;
        }

        private void Update()
        {
            UpdateSize();

            var target = tank.turretRotation;
            var force = DeltaAngle(target, lagPosition) * lagSpring - lagVelocity * lagDamping;

            lagPosition += lagVelocity * Time.deltaTime;
            lagVelocity += force * Time.deltaTime;

            lagPosition.x = (lagPosition.x % 360f + 360f) % 360f;
            lagPosition.y = (lagPosition.y % 360f + 360f) % 360f;

            var offset = DeltaAngle(lagPosition, tank.turretRotation);
            offset = offset.normalized * Mathf.Atan(offset.magnitude * lagSlope) * 2f / Mathf.PI * lagMax;

            rectTransform.anchoredPosition = offset;
        }

        private Vector2 DeltaAngle(Vector2 a, Vector2 b) => new()
        {
            x = Mathf.DeltaAngle(b.x, a.x),
            y = Mathf.DeltaAngle(b.y, a.y),
        };

        private void UpdateSize()
        {
            if (canvas == null) return;
            var width = canvas.pixelRect.width;
            rectTransform.sizeDelta = new Vector2(width + texturePad, width + texturePad);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                canvas = GetComponentInParent<Canvas>();
                UpdateSize();
            }
        }
    }
}