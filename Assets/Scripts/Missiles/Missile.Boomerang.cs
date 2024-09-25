public static partial class Missiles {
    public static ID BoomerangID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Boomerang() => new() {
        gfx = new() {
            spriteName = "boomerang",
        },
        missile = new() {
            Damage = 15,
            Speed = 1f,
            Acceleration = 2,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -1),
            Flags =
                MissileFlags.GrowsOnBirth
                | MissileFlags.ShrinksOnDying
                | MissileFlags.Spins
                ,
            HitEffect = HitEffect.Normal,
            MoveType = MissileMoveType.Boomerang,
            ScaleEnd = 1f,
            Radius = 0.3f,
            HitType = MissileHitType.AoE_Circle2Rect,
            spinSpeedMultiplier = 2,
            StatusEffects = new() {
                new() {
                    HitEffect = HitEffect.Knockback,
                    Duration = 1f,
                },
            },
        }
    };
}