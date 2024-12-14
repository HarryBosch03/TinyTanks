using System;
using TinyTanks.Health;
using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmComponent : MonoBehaviour
    {
        public int maxHealth;
        public Color baseColor = Color.white;
        public float flashFrameRate = 12f;
        public float flashBrightness = 6f;

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
            if (!Application.isPlaying) return;
            
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                destroyed = true;
            }
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