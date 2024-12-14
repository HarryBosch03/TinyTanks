using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Health
{
    public interface ICanBeDamaged
    {
        public bool canRicochet { get; }
        public int defense { get; }
        public void Damage(DamageInstance damage, DamageSource source, out DamageReport report);

        protected static void NotifyDamaged(GameObject victim, DamageInstance damage, DamageSource source, DamageReport report) => DamagedEvent?.Invoke(victim, damage, source, report);
        public static event System.Action<GameObject, DamageInstance, DamageSource, DamageReport> DamagedEvent;

        public struct DamageReport : INetworkSerializable
        {
            public bool canPenetrate;
            public Ray entryRay;
            public bool didRicochet;
            public Ray exitRay;
            public SpallReport[] spall;

            public struct SpallReport : INetworkSerializable
            {
                public static readonly SpallReport None = new SpallReport { hitComponent = "", };

                public Ray ray;
                public bool didHit;
                public Vector3 hit;
                public string hitComponent;

                public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
                {
                    serializer.SerializeValue(ref ray);
                    serializer.SerializeValue(ref didHit);
                    serializer.SerializeValue(ref hit);
                    serializer.SerializeValue(ref hitComponent);
                }
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

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref canPenetrate);
                serializer.SerializeValue(ref entryRay);
                serializer.SerializeValue(ref didRicochet);
                serializer.SerializeValue(ref exitRay);
                serializer.SerializeValue(ref spall);
            }
        }
    }
}