public static partial class Missiles {
    public static ID MeteorID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Meteor() => new() {
        gfx = new() {
            spriteName = "MeteorMissile",
        },
        missile = new() {
            Speed = 7f,
            Flags = MissileFlags.Spins | MissileFlags.NoDespawn | MissileFlags.Explodes | MissileFlags.IsDisarmed | MissileFlags.IgnoresSpeed,
            HitType = MissileHitType.AoE_Circle2Rect,
            spawnOffset = new Unity.Mathematics.float3(10, ScreenBounds.Outer_Y_Max, -0.5f),
            ScaleEnd = 2,
            Radius = 0.5f,
            HitEffect = HitEffect.Fire,
        }
    };

    public static SubMissileDeath MeteorSubMissile() => new() {
        missileToSpawnID = ExplosionID,
    };

    public static SubMissileDeath MeteorSubMissile2() => new() {
        missileToSpawnID = FireFieldID,
    };
}