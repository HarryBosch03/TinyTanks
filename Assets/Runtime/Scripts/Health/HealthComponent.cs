using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TinyTanks.Tanks;
using UnityEngine;

namespace TinyTanks.Health
{
    public class HealthComponent : NetworkBehaviour, ICanBeDamaged
    {
        public int maxHealth;
        public int defenseEditor;
        public bool canRicochetEditor = true;

        private TankController tankController;
        
        public bool canRicochet => canRicochetEditor;
        public int defense => defenseEditor;
        public readonly SyncVar<int> currentHealth = new SyncVar<int>();

        private void Awake()
        {
            tankController = GetComponentInParent<TankController>();
        }

        public override void OnStartServer()
        {
            currentHealth.Value = maxHealth;
        }
        
        [Server]
        public void Damage(DamageInstance damage, DamageSource source)
        {
            var finalDamage = Mathf.Max(damage.damage, defense);
            if (source.critical) finalDamage *= 2;
            
            currentHealth.Value -= finalDamage;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                // Destroy
            }
            NotifyDamageRpc(damage, source, finalDamage);
        }

        [ObserversRpc(RunLocally = false)]
        private void NotifyDamageRpc(DamageInstance damage, DamageSource source, int finalDamage)
        {
            Debug.Log($"{tankController.name} was hit by {source.invoker} for {finalDamage} damage");
        }
    }
}