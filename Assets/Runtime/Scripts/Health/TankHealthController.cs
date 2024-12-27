using System;
using TinyTanks.Tanks;
using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Health
{
    [RequireComponent(typeof(TankController))]
    public class TankHealthController : NetworkBehaviour, ICanBeDamaged
    {
        public int maxHealth;
        public int currentHealth;
        public int baseDefense;
        public int baseArmorClass;

        private TankController tank;

        public event Action<DamageInstance, DamageSource, ICanBeDamaged.DamageReport> DamagedEvent;

        public void DamageDirect(DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report)
        {
            currentHealth -= report.finalDamage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                tank.SetIsDestroyed(true);
            }

            NotifyDamamgedClientRpc(damage, source, report, currentHealth);
        }

        public void Damage(DamageInstance damage, DamageSource source, out ICanBeDamaged.DamageReport report)
        {
            ICanBeDamaged.CalculateDamage(this, damage, source, out report, baseDefense, baseArmorClass, true);
            DamageDirect(damage, source, report);
        }

        [ClientRpc]
        private void NotifyDamamgedClientRpc(DamageInstance damage, DamageSource source, ICanBeDamaged.DamageReport report, int currentHealth)
        {
            this.currentHealth = currentHealth;
            DamagedEvent?.Invoke(damage, source, report);

            source.invoker.TryGet(out var invoker);
            Debug.Log($"{(invoker != null ? invoker.name : "<null>")} damaged {name} for {report.finalDamage}({damage.damageAmount})\nAngle: {source.impactAngle}");
        }

        private void Awake()
        {
            tank = GetComponent<TankController>();
            currentHealth = maxHealth;
        }

        private void OnEnable() { tank.SetIsDestroyedEvent += OnSetIsDestroyed; }

        private void OnDisable() { tank.SetIsDestroyedEvent -= OnSetIsDestroyed; }

        private void OnSetIsDestroyed(bool isDestroyed)
        {
            if (IsServer && !isDestroyed)
            {
                currentHealth = maxHealth;
            }
        }
    }
}