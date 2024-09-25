public static partial class Missiles {
    public static ID FamiliarExplosionID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FamiliarExplosion() => new() {
        gfx = new() {
            spriteName = "none",
        },
        missile = new() {
            Damage = 15,
            Duration = 0.75f,
            Flags = MissileFlags.NoDespawn | MissileFlags.HasTimedLife,
            HitType = MissileHitType.AoE_Circle2Rect,
            MoveType = MissileMoveType.Stationary,
            ScaleEnd = 0.75f,
            Radius = 0.25f,
            HitFrequency = 10,
            HitEffect = HitEffect.Fire,
        }
    };
}