public static partial class Missiles {
    public static ID FrozenOrbID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FrozenOrb() => new() {
        gfx = new() {
            spriteName = "FrozenOrb_Missile",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
        },
        missile = new() {
            Radius = 0.5f,
            Damage = 100,
            Duration = 5,
            Speed = 1.8f,
            Flags = MissileFlags.IsFamiliar | MissileFlags.DontUpdateStats | MissileFlags.HasTimedLife | MissileFlags.Spins | MissileFlags.ShrinksOnDying | MissileFlags.NoDespawn | MissileFlags.GrowsOnBirth,
            ScaleEnd = 1.5f,
            MoveType = MissileMoveType.Forward,
            HitType = MissileHitType.AoE_Circle2Rect,
            spinSpeedMultiplier = 3,

            StatusEffects = new() {
                    new() {
                        HitEffect = HitEffect.Knockback,
                        Duration = 1.5f,
                    },
                },
        }
    };
}