namespace TinyTanks.Health
{
    public interface ICanBeDamaged
    {
        bool canRicochet { get; }
        int defense { get; }
        void Damage(DamageInstance damage, DamageSource source);
    }
}