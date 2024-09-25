public static partial class Missiles {
    public static ID ClockStopID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype ClockStop() => new() {
        gfx = new() {
            spriteName = "graphic2",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
            Flags = OffsetFlags.NoParticlesOffset,
        },
        missile = new() {
            Duration = 0.25f,
            ScaleEnd = 1.5f,
            Flags =
                MissileFlags.GrowsOnBirth
                | MissileFlags.HasTimedLife
                | MissileFlags.IgnoresDuration
                ,
            HitType = MissileHitType.AoE_RectRotation,
            MoveType = MissileMoveType.Stationary,
            Radius = 1,
            HitFrequency = 1f,
            HitEffect = HitEffect.Freeze,
            StatusEffects = new() {
                new() {
                    HitEffect = HitEffect.Freeze,
                    Duration = 2f,
                },
            },
        }
    };
}