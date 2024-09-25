public static partial class Missiles {
    public static ID DivineShieldID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype DivineShield() => new() {
        gfx = new() {
            spriteName = "DivineShield",
        },
        movement = new() {
            OffsetType = OffsetType.NoPlayerOffset,
        },
        missile = new() {
            Duration = 2,
            Radius = 0.5f,
            spawnOffset = new Unity.Mathematics.float3(0, 0.5f, -0.5f),
            Flags = MissileFlags.NoDespawn
                | MissileFlags.HasTimedLife
                | MissileFlags.IgnoresArea
                | MissileFlags.ShrinksOnDying
                | MissileFlags.GrowsOnBirth
                | MissileFlags.MakesPlayerInvulnerable
                ,
            ScaleEnd = 1.2f,
            MoveType = MissileMoveType.Stationary,
        }
    };
}