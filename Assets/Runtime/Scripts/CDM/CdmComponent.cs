using System;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TinyTanks.Health;
using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmComponent : NetworkBehaviour
    {
        public int maxHealth;
        public Color baseColor = Color.white;
        public float flashFrameRate = 12f;
        public float flashBrightness = 6f;

        private float alpha;
        private ICdmShape[] shapes;
        private Renderer[] renderers;

        public readonly SyncVar<bool> destroyed = new SyncVar<bool>(ICanBeDamaged.HealthSyncSettings);
        public readonly SyncVar<int> currentHealth = new SyncVar<int>(ICanBeDamaged.HealthSyncSettings);
        
        public string displayName => name;
        public float visibleTime { get; set; }
        public float flashTime { get; set; }
        
        private void Awake()
        {
            shapes = GetComponentsInChildren<ICdmShape>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        public override void OnStartServer()
        {
            currentHealth.Value = maxHealth;
            destroyed.Value = false;
        }

        public override void OnStartNetwork()
        {
            ICanBeDamaged.DamagedEvent += OnDamaged;
        }

        private void OnDestroy()
        {
            for (var i =0 ; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(new MaterialPropertyBlock());
            }
        }

        private void Update()
        {
            if (visibleTime > 0f) alpha = 1f;
            else alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime);
            
            UpdateRenderers();
            
            visibleTime -= Time.deltaTime;
            flashTime -= Time.deltaTime;
        }

        public void Damage(int damage)
        {
            if (Application.isPlaying && !IsServerStarted) return;
            
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                destroyed.Value = true;
            }
        }

        private void UpdateRenderers()
        {
            var propertyBlock = new MaterialPropertyBlock();
            var health = Mathf.Clamp01((float)currentHealth.Value / maxHealth);
            var color = Color.Lerp(Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 1f), baseColor, Mathf.Sin(Time.time) * 0.5f + 0.5f), baseColor, health);
            color.a = alpha * health;
            if (destroyed.Value) color = new Color(0.5f, 0.5f, 0.5f, 0.5f * alpha);
            if (flashTime > 0f) color = Color.white * flashBrightness;
            
            propertyBlock.SetColor("_BaseColor", color);
            
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        public bool DoesIntersect(Ray ray, out float enter0)
        {
            if (!Application.isPlaying)
            {
                shapes = GetComponentsInChildren<ICdmShape>();
            }

            enter0 = float.MaxValue;
            var didHit = false;
            for (var i = 0; i < shapes.Length; i++)
            {
                if (shapes[i].DoesIntersect(ray, out var enter1))
                {
                    didHit = true;
                    enter0 = Mathf.Min(enter0, enter1);
                }
            }

            return didHit;
        }
        
        private void OnDamaged(NetworkObject victim, DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            if (transform.IsChildOf(victim.transform) && Array.Exists(report.spall, e => e.hitComponent == name))
            {
                visibleTime = 4f;
                flashTime = 1f / flashFrameRate;
            }
        }
    }
}