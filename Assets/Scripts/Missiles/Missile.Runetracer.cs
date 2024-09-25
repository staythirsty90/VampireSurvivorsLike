public static partial class Missiles {
    public static ID RunetracerID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Runetracer() => new() {
        gfx = new() {
            spriteName = "RunetracerMissile",
        },
        missile = new() {
            Damage = 10,
            Speed = 1f,
            Duration = 2.25f,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -0.5f),
            Flags = MissileFlags.HasTimedLife | MissileFlags.BouncesOffWalls | MissileFlags.ShrinksOnDying | MissileFlags.GrowsOnBirth | MissileFlags.BouncesOffObstructibles | MissileFlags.Spins,
            ScaleEnd = 1.5f,
            HitType = MissileHitType.AoE_Rect,
            HitEffect = HitEffect.Freeze,
            Radius = 0.15f,
            HitFrequency = 0.5f,
        }
    };
}