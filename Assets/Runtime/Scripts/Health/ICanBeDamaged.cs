using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Health
{
    public interface ICanBeDamaged
    {
        public void Damage(DamageInstance damage, DamageSource source, out DamageReport report);

        protected static void NotifyDamaged(GameObject victim, DamageInstance damage, DamageSource source, DamageReport report) => DamagedEvent?.Invoke(victim, damage, source, report);
        public static event System.Action<GameObject, DamageInstance, DamageSource, DamageReport> DamagedEvent;

        public static void CalculateDamage(NetworkBehaviour netBehaviour, DamageInstance damage, DamageSource source, out DamageReport report, int defense, int armorClass)
        {
            if (!netBehaviour.IsServer)
            {
                report = default;
                return;
            }

            report.finalDamage = damage.damageAmount;

            report.didPenetrate = damage.damageClass >= armorClass;
            if (!report.didPenetrate)
            {
                report.didCrit = false;
                report.finalDamage = 0;
                return;
            }

            report.didCrit = source.impactAngle < damage.critAngle;
            if (report.didCrit) report.finalDamage *= 3;
            report.finalDamage -= defense;
            report.finalDamage = Mathf.Max(report.finalDamage, 1);
        }
        
        public struct DamageReport : INetworkSerializable
        {
            public int finalDamage;
            public bool didCrit;
            public bool didPenetrate;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref finalDamage);
                serializer.SerializeValue(ref didCrit);
                serializer.SerializeValue(ref didPenetrate);
            }
        }
    }
}