public static partial class Missiles {
    public static ID FamiliarDamageZoneID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype FamiliarDamageZone() => new() {
        gfx = new() {
            spriteName = "none",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
            Flags = OffsetFlags.NoParticlesOffset,
        },
        missile = new() {
            Speed = 0.1f,
            Flags = MissileFlags.ShrinksOnDying | MissileFlags.IsFamiliarDamageZone | MissileFlags.IsDisarmed | MissileFlags.NoDespawn | MissileFlags.IgnoreScaling,
            MoveType = MissileMoveType.Orbit,
            ScaleEnd = 1.5f,
            Radius = 1.6f,
            distanceFromCenter = 20f,
        }
    };
}