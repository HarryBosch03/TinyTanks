using TinyTanks.Health;
using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Level
{
    public class SurfaceHitEffect : NetworkBehaviour
    {
        public ParticleSystem hitEffect;
        public ParticleSystem ricochetEffect;
        public ParticleSystem smallHitEffect;
        public ParticleSystem smallRicochetEffect;
        
        public void Play(DamageSource source, ICanBeDamaged.DamageReport report)
        {
            if (!IsServer) return;
            PlayRpc(source, report);
        }
        
        private void Awake()
        {
            InitEffect(ref hitEffect);
            InitEffect(ref ricochetEffect);
            InitEffect(ref smallHitEffect);
            InitEffect(ref smallRicochetEffect);
        }

        private void InitEffect(ref ParticleSystem system)
        {
            if (system == null) return;
            system = Instantiate(system, transform);

            foreach (var subSystem in system.GetComponentsInChildren<ParticleSystem>())
            {
                var main = subSystem.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayRpc(DamageSource source, ICanBeDamaged.DamageReport report)
        {
            ParticleSystem effect;

            if (source.useSmallEffect)
            {
                effect = report.didRicochet ? smallRicochetEffect : smallHitEffect;
            }
            else
            {
                effect = report.didRicochet ? ricochetEffect : hitEffect;
            }

            if (effect == null) return;
            
            effect.transform.position = source.hitPoint;
            effect.transform.rotation = Quaternion.LookRotation(source.hitNormal, source.direction);
            effect.Play(true);
        }
    }
}