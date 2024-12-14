using TinyTanks.Health;
using TinyTanks.Utility;
using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmController : NetworkBehaviour, ICanBeDamaged
    {
        public int armorDefense;
        public ParticleSystem spallParticles;

        private CdmComponent[] components;
        private Collider[] collision;

        public bool canRicochet => true;
        public int defense => armorDefense;

        private void Awake()
        {
            collision = GetComponentsInChildren<Collider>();
            components = GetComponentsInChildren<CdmComponent>();
        }

        public void Damage(DamageInstance damage, DamageSource source, out ICanBeDamaged.DamageReport report)
        {
            if (!Application.isPlaying)
            {
                collision = GetComponentsInChildren<Collider>();
                components = GetComponentsInChildren<CdmComponent>();
            }
            else if (!IsServer)
            {
                report = default;
                return;
            }
            
            report.entryRay = new Ray(source.origin, source.direction);
            report.exitRay = new Ray(source.hitPoint, (source.direction - source.hitNormal).normalized);

            var angle = Mathf.Cos(Vector3.Dot(-source.hitNormal, source.direction)) * Mathf.Rad2Deg;
            
            if (damage.damageClass < defense)
            {
                report.canPenetrate = false;
                report.didRicochet = false;
                report.spall = new ICanBeDamaged.DamageReport.SpallReport[0];
            }
            else if (angle > damage.ricochetAngle)
            {
                report.exitRay.direction = Vector3.Reflect(report.entryRay.direction, source.hitNormal);
                report.canPenetrate = true;
                report.didRicochet = true;
                report.spall = new ICanBeDamaged.DamageReport.SpallReport[0];
            }
            else
            {
                var rng = new System.Random(0);
                report.spall = new ICanBeDamaged.DamageReport.SpallReport[damage.spallCount];
                report.canPenetrate = true;
                report.didRicochet = false;

                for (var i = 0; i < report.spall.Length; i++)
                {
                    ref var spall = ref report.spall[i];
                    spall = ICanBeDamaged.DamageReport.SpallReport.None;
                    var orientation = Quaternion.LookRotation(report.exitRay.direction);
                    orientation *= Quaternion.Euler(rng.NextFloat(-damage.spallAngle, damage.spallAngle), rng.NextFloat(-damage.spallAngle, damage.spallAngle), 0f);

                    spall.ray = new Ray(report.exitRay.origin, orientation * Vector3.forward);

                    var hitComponent = (CdmComponent)null;
                    var hitEnter = float.MaxValue;
                    for (var j = 0; j < components.Length; j++)
                    {
                        var component = components[j];
                        if (!component.destroyed && component.DoesIntersect(spall.ray, out var enter) && enter < hitEnter)
                        {
                            hitEnter = enter;
                            hitComponent = component;
                            spall.didHit = true;
                        }
                    }

                    if (!spall.didHit)
                    {
                        for (var j = 0; j < collision.Length; j++)
                        {
                            if (collision[j].Raycast(new Ray(spall.ray.origin + spall.ray.direction * 10f, -spall.ray.direction), out var spallHit, 10f))
                            {
                                hitEnter = Mathf.Min(hitEnter, Vector3.Dot(spall.ray.direction, spallHit.point - spall.ray.origin));
                                spall.didHit = true;
                            }
                        }
                    }

                    spall.hit = spall.ray.GetPoint(hitEnter);
                    if (hitComponent != null)
                    {
                        spall.hitComponent = hitComponent.name;
                        hitComponent.Damage(1);
                        
                        if (spallParticles != null)
                        {
                            spallParticles.Emit(new ParticleSystem.EmitParams
                            {
                                position = spallParticles.transform.InverseTransformPoint(spall.hit),
                            }, 1);
                        }
                    }
                }
            }
            
            report.DebugDraw();
            if (Application.isPlaying) NotifyDamagedRpc(damage, source, report);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifyDamagedRpc(DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            ICanBeDamaged.NotifyDamaged(gameObject, damage, source, report);
        }

        public CdmComponent GetComponentFromName(string name)
        {
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i].name == name) return components[i];
            }

            return null;
        }
    }
}