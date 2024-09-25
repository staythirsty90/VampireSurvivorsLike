public static partial class Missiles {
    public static ID WhipID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Whip() => new() {
        gfx = new() {
            spriteName = "graphic3",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
        },
        missile = new() {
            Damage = 10,
            Duration = 0.3f,
            ScaleEnd = 1f,
            Flags = MissileFlags.Flips | MissileFlags.GrowsOnBirth | MissileFlags.HasTimedLife | MissileFlags.IgnoresDuration,
            HitType = MissileHitType.AoE_RectRotation,
            HitEffect = HitEffect.Normal,
            Radius = 1,
            spawnOffset = new Unity.Mathematics.float3(1.5f, 0, 0),
        }
    };
}