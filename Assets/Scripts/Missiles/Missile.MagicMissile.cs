public static partial class Missiles {
    public static ID MagicMissileID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype MagicMissile() => new() {
        gfx = new() {
            spriteName = "MyGlow",
        },
        missile = new() {
            Damage = 10,
            Speed = 1f,
            Flags = MissileFlags.IsPiercing | MissileFlags.ShrinksOnDying,
            ScaleEnd = 2,
            HitType = MissileHitType.AoE_Circle2Rect,
            HitEffect = HitEffect.Freeze,
            Radius = 0.2f,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -0.5f),
        }
    };
}