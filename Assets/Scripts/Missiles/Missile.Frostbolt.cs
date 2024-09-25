public static partial class Missiles {
    public static ID FrostboltID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Frostbolt() => new() {
        gfx = new() {
            spriteName = "_Frostbolt",
        },
        missile = new() {
            MoveType = MissileMoveType.Forward,
            Damage = 1,
            Speed = 1,
            Duration = 6,
            //SpawnPosition = SpawnPosition.AtPlayer,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -0.5f),
            Flags = MissileFlags.RotateDirection | MissileFlags.GrowsOnBirth | MissileFlags.HasTimedLife | MissileFlags.ShrinksOnDying | MissileFlags.IsPiercing,
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
        }
    };
}