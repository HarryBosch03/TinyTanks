using System;
using UnityEngine;

namespace TinyTanks.Health
{
    public class HealthCollider : MonoBehaviour, ICanBeDamaged
    {
        public int defense;
        public int armorClass;

        private TankHealthController health;

        private void Awake()
        {
            health = GetComponentInParent<TankHealthController>();
        }

        public void Damage(DamageInstance damage, DamageSource source, out ICanBeDamaged.DamageReport report)
        {
            ICanBeDamaged.CalculateDamage(health, damage, source, out report, defense, armorClass);
            health.DamageDirect(damage, source, report);
        }
    }
}