public static partial class Missiles {
    public static ID FireFieldID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FireField() => new() {
        gfx = new() {
            spriteName = "none",
        },
        missile = new() {
            Duration = 3,
            Damage = 5,
            Flags = MissileFlags.NoDespawn | MissileFlags.GrowsOnBirth | MissileFlags.HasTimedLife | MissileFlags.ShrinksOnDying,
            HitType = MissileHitType.AoE_Circle2Rect,
            MoveType = MissileMoveType.Stationary,
            ScaleEnd = 1f,
            Radius = 1f,
            HitFrequency = 0.5f,
            HitEffect = HitEffect.Fire,
        }
    };
}