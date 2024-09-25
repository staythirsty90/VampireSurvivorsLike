public static partial class Missiles {

    public static ID AxeID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Axe() => new() {
        gfx = new() {
            spriteName = "Axe",
        },
        missile = new() {
            Damage = 20,
            Speed = 1,
            Duration = 2,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -1),
            Flags =
              MissileFlags.GrowsOnBirth
            | MissileFlags.ShrinksOnDying
            | MissileFlags.HasTimedLife
            | MissileFlags.Spins
            | MissileFlags.IsPiercing
            | MissileFlags.Flips
            ,
            Piercing = 3,
            HitEffect = HitEffect.Normal,
            MoveType = MissileMoveType.Arc,
            Acceleration = 2,
            ScaleEnd = 0.75f,
            Radius = 0.45f,
            spinSpeedMultiplier = 0.5f,
            HitType = MissileHitType.AoE_Circle2Rect,
        }
    };
}