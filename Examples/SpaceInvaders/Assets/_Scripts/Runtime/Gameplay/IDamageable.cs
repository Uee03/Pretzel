namespace SpaceInvaders.Gameplay
{
    /// <summary>Anything a projectile can hit. Triggers look for this interface, so projectiles never know concrete targets.</summary>
    public interface IDamageable
    {
        /// <returns>True when the hit counted, so the projectile is spent. False lets it fly on (friendly fire, invulnerability).</returns>
        bool TakeHit(Team attacker);
    }
}
