using System;
using TinyTanks.Tanks;
using TinyTanks.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTanks.Health
{
    [RequireComponent(typeof(RectTransform))]
    public class Healthbar : MonoBehaviour
    {
        public Image outline;
        public Image background;
        public Image fill;
        public float shakeFrequency;
        public float shakeAmplitude;
        public float shakeDecayTime;
        
        public RectTransform rectTransform => transform as RectTransform;

        private TankController tank;
        private TankHealthController health;
        private float lastDamagedTime;
        private Vector3 restPosition;
        private Camera mainCamera;
        private ICanBeDamaged.DamageReport lastDamageReport;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            health = GetComponentInParent<TankHealthController>();
            restPosition = transform.localPosition;
            mainCamera = Camera.main;

            tank.SetIsDestroyedEvent += OnSetIsDestroyed;
        }

        private void OnSetIsDestroyed(bool isDestroyed)
        {
            gameObject.SetActive(!isDestroyed);
        }

        private void OnEnable()
        {
            health.DamagedEvent += OnDamaged;
            tank.ActiveViewerChangedEvent += OnActiveViewerChanged;
        }

        private void OnDisable()
        {
            health.DamagedEvent -= OnDamaged;
            tank.ActiveViewerChangedEvent -= OnActiveViewerChanged;
        }

        private void OnActiveViewerChanged(bool isActiveViewer)
        {
            gameObject.SetActive(!isActiveViewer);
        }

        private void LateUpdate()
        {
            fill.fillAmount = (float)health.currentHealth / health.maxHealth;

            transform.rotation = mainCamera.transform.rotation;
            var shakeAmplitude = this.shakeAmplitude * lastDamageReport.finalDamage;
            var angle = Mathf.PerlinNoise1D(Time.time * shakeFrequency) * Mathf.PI * 2f;
            var shakePosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Sin(Time.time * Mathf.PI * shakeFrequency) * shakeAmplitude * DecayCurve((Time.time - lastDamagedTime) / shakeDecayTime);
            transform.localPosition = restPosition;
            transform.position += transform.rotation * shakePosition;
        }

        private static float DecayCurve(float x) => Mathf.Pow(2f, -x);

        private void OnDamaged(DamageInstance instance, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            lastDamagedTime = Time.time;
            lastDamageReport = report;
        }
    }
}