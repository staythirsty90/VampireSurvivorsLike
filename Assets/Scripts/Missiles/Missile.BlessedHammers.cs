public static partial class Missiles {

    public static ID BlessedHammersID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype BlessedHammers() => new() {
        gfx = new() {
            spriteName = "_Hammer",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
        },
        missile = new() {
            Damage = 10,
            Speed = 1f,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -0.5f),
            Duration = 3,
            Flags = MissileFlags.Spins | MissileFlags.GrowsOnBirth | MissileFlags.ShrinksOnDying | MissileFlags.HasTimedLife,
            HitEffect = HitEffect.Normal,
            MoveType = MissileMoveType.Orbit,
            ScaleEnd = 2f,
            Radius = 0.35f,
            HitFrequency = 1.7f,
            HitType = MissileHitType.AoE_Circle2Rect,
            StatusEffects = new() {
                new() {
                    HitEffect = HitEffect.Knockback,
                    Duration = 1f,
                },
            },
        }
    };
}