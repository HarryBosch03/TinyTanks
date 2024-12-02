using FishNet.Object;
using FishNet.Object.Synchronizing;
using TinyTanks.CDM;
using UnityEngine;

namespace TinyTanks.Health
{
    public interface ICanBeDamaged
    {
        public bool canRicochet { get; }
        public int defense { get; }
        public void Damage(DamageInstance damage, DamageSource source, out DamageReport report);

        protected static void NotifyDamaged(NetworkObject victim, DamageInstance damage, DamageSource source, DamageReport report) => DamagedEvent?.Invoke(victim, damage, source, report);
        public static event System.Action<NetworkObject, DamageInstance, DamageSource, DamageReport> DamagedEvent;

        public static readonly SyncTypeSettings HealthSyncSettings = new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers);

        public struct DamageReport
        {
            public bool canPenetrate;
            public Ray entryRay;
            public bool didRicochet;
            public Ray exitRay;
            public SpallReport[] spall;

            public struct SpallReport
            {
                public Ray ray;
                public bool didHit;
                public Vector3 hit;
                public string hitComponent;
            }

            public void DrawGizmos()
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(entryRay.origin, exitRay.origin);
                Gizmos.DrawRay(exitRay.origin, exitRay.direction * 10f);

                if (spall != null)
                {
                    for (var i = 0; i < spall.Length; i++)
                    {
                        if (spall[i].didHit)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(spall[i].ray.origin, spall[i].hit);
                            Gizmos.color = Color.white;
                            Gizmos.DrawSphere(spall[i].hit, 0.01f);
                        }
                        else
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(spall[i].ray.origin, spall[i].ray.direction * 10f);
                        }
                    }
                }
            }

            public void DebugDraw()
            {
                if (!Application.isPlaying) return;
                
                Debug.DrawLine(entryRay.origin, exitRay.origin, Color.yellow, 5f);
                Debug.DrawRay(exitRay.origin, exitRay.direction * 10f, Color.yellow, 5f);

                if (spall != null)
                {
                    for (var i = 0; i < spall.Length; i++)
                    {
                        if (spall[i].didHit)
                        {
                            Debug.DrawLine(spall[i].ray.origin, spall[i].hit, Color.red, 5f);
                        }
                        else
                        {
                            Debug.DrawRay(spall[i].ray.origin, spall[i].ray.direction * 10f, Color.red, 5f);
                        }
                    }
                }
            }
        }
    }
}