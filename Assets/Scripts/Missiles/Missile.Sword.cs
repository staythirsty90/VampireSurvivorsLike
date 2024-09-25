using UnityEngine;

public static partial class Missiles {
    public static ID SwordID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Sword() => new() {
        gfx = new() {
            spriteName = "sword",
        },
        missile = new() {
            MoveType = MissileMoveType.Forward,
            Damage = 7,
            Speed = 1,
            spawnOffset = new Unity.Mathematics.float3(0, 0.3f, -0.5f),
            Flags = MissileFlags.RotateDirection | MissileFlags.GrowsOnBirth | MissileFlags.ShrinksOnDying | MissileFlags.IsPiercing | MissileFlags.BouncesOffObstructibles,
            HitEffect = HitEffect.Normal,
            ScaleEnd = 1f,
            Radius = 0.35f,
            HitType = MissileHitType.AoE_RectRotation,
        }
    };
}