using System;
using TinyTanks.Health;
using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmComponent : NetworkBehaviour
    {
        public int maxHealth;
        public Color baseColor = Color.white;
        public float flashFrameRate = 12f;
        public float flashBrightness = 6f;
        public bool isFlesh;
        public bool isCritical;
        public bool requiredToDrive;
        public bool requiredToShoot;

        private float alpha;
        private ICdmShape[] shapes;
        private Renderer[] renderers;

        public string displayName => name;
        public float visibleTime { get; set; }
        public float flashTime { get; set; }
        public bool destroyed { get; private set; }
        public int currentHealth { get; private set; }
        
        private void Awake()
        {
            shapes = GetComponentsInChildren<ICdmShape>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnEnable()
        {
            currentHealth = maxHealth;
            destroyed = false;
            ICanBeDamaged.DamagedEvent += OnDamaged;
        }

        public override void OnDestroy()
        {
            for (var i =0 ; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(new MaterialPropertyBlock());
            }
            
            base.OnDestroy();
        }

        public void Destroy() => Damage(int.MaxValue);

        public void FullRepair() => Repair(int.MaxValue);

        private void Update()
        {
            if (visibleTime > 0f) alpha = 1f;
            else alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime);
            
            UpdateRenderers();
            
            visibleTime -= Time.deltaTime;
            flashTime -= Time.deltaTime;
        }

        public void Repair(int health)
        {
            if (!IsServer) return;
            if (!Application.isPlaying) return;

            destroyed = false;
            currentHealth += health;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthClientRpc(currentHealth, destroyed);
        }

        public void Damage(int damage)
        {
            if (!IsServer) return;
            if (!Application.isPlaying) return;
            
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                destroyed = true;
            }

            UpdateHealthClientRpc(currentHealth, destroyed);
        }

        [ClientRpc]
        private void UpdateHealthClientRpc(int currentHealth, bool destroyed)
        {
            this.currentHealth = currentHealth;
            this.destroyed = destroyed;
        }

        private void UpdateRenderers()
        {
            var propertyBlock = new MaterialPropertyBlock();
            var health = Mathf.Clamp01((float)currentHealth / maxHealth);
            var color = Color.Lerp(Color.Lerp(new Color(0.5f, 0.5f, 0.5f, 1f), baseColor, Mathf.Sin(Time.time) * 0.5f + 0.5f), baseColor, health);
            color.a = alpha * health;
            if (destroyed) color = new Color(0.5f, 0.5f, 0.5f, 0.5f * alpha);
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
        
        private void OnDamaged(GameObject victim, DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            if (transform.IsChildOf(victim.transform) && Array.Exists(report.spall, e => e.hitComponent == name))
            {
                visibleTime = 4f;
                flashTime = 1f / flashFrameRate;
            }
        }
    }
}