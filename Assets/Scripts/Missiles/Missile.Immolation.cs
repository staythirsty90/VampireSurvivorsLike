public static partial class Missiles {
    public static ID ImmolationID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Immolation() => new() {
        gfx = new() {
            spriteName = "none",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
            Flags = OffsetFlags.NoParticlesOffset,
        },
        missile = new() {
            Damage = 5,
            Flags = MissileFlags.NoDespawn,
            ScaleEnd = 1,
            HitType = MissileHitType.AoE_Circle2Rect,
            HitEffect = HitEffect.Fire,
            MoveType = MissileMoveType.Stationary,
            Radius = 1,
            HitFrequency = 1.33f,
            StatusEffects = new() {
                    new() {
                        HitEffect = HitEffect.Knockback,
                        Duration = 2f,
                    },
                },
        }
    };
}