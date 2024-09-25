public static partial class Missiles {
    public static ID FrozenOrbFrostboltID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FrozenOrbFrostbolt() => new() {
        gfx = new() {
            spriteName = "_Frostbolt",
        },
        missile = new() {
            MoveType = MissileMoveType.Forward,
            Damage = 5,
            Speed = 1f,
            Duration = 2,
            Flags = MissileFlags.RotateDirection | MissileFlags.HasTimedLife | MissileFlags.ShrinksOnDying | MissileFlags.IsPiercing | MissileFlags.GrowsOnBirth,
            HitEffect = HitEffect.Freeze,
            ScaleEnd = 2f,
            Radius = 0.35f,
            HitType = MissileHitType.AoE_Rect,

            StatusEffects = new() {
                    new() {
                        HitEffect = HitEffect.Freeze,
                        Duration = 1.5f,
                    },
                },

            Piercing = 5,
            HitFrequency = 0.33f,
            //KnockBack = 1,
        }
    };
}