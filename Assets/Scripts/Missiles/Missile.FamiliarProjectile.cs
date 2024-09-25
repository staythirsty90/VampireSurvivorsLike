public static partial class Missiles {
    public static ID FamiliarProjectileID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FamiliarProjectile() => new() {
        gfx = new() {
            spriteName = "none",
        },
        missile = new() {
            Speed = 6f,
            Flags = MissileFlags.RotateDirection | MissileFlags.NoDespawn | MissileFlags.Explodes | MissileFlags.IsDisarmed | MissileFlags.IgnoresSpeed | MissileFlags.ShrinksOnDying,
            HitType = MissileHitType.AoE_Circle2Rect,
            ScaleEnd = 1f,
            Radius = 0.5f,
            HitEffect = HitEffect.Fire,
        }
    };

    public static SubMissileDeath FamiliarProjectileSubMissile() => new() {
        missileToSpawnID = FamiliarExplosionID,
    };
}