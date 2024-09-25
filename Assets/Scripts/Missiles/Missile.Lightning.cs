public static partial class Missiles {
    public static ID LightningID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Lightning() => new() {
        gfx = new() {
            spriteName = "none",
        },
        movement = new() {
            Flags = OffsetFlags.NoParticlesOffset,
        },
        missile = new() {
            Damage = 15,
            Duration = 0.5f,
            ScaleEnd = 3,
            Flags = MissileFlags.NoDespawn | MissileFlags.HasTimedLife | MissileFlags.IgnoresDuration,
            HitEffect = HitEffect.Electric,
            Radius = 0.25f,
            SpawnDirection = Directions.None,
            MoveType = MissileMoveType.Stationary,
            HitFrequency = 1,
            HitType = MissileHitType.AoE_Circle2Circle,
        }
    };
}