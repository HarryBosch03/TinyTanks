using TinyTanks.Health;
using UnityEngine;

namespace TinyTanks.Level
{
    public class InvokeDamageEffect : MonoBehaviour, ICanBeDamaged
    {
        public SurfaceHitEffect effect;

        public void Damage(DamageInstance damage, DamageSource source, out ICanBeDamaged.DamageReport report)
        {
            report.didCrit = false;
            report.didPenetrate = false;
            report.finalDamage = 0;
            report.didRicochet = false;

            if (effect != null) effect.Play(source, report);
        }
    }
}