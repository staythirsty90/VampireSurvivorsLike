public static partial class Missiles {
    public static ID FamiliarID = new() { Guid = System.Guid.NewGuid() };

    static MissileArchetype Familiar() => new() {
        gfx = new() {
            spriteName = "bird",
        },
        missile = new() {
            Radius = 1,
            Flags = MissileFlags.NoDespawn | MissileFlags.IsFamiliar | MissileFlags.DontUpdateStats | MissileFlags.FacesCenter,
            ScaleEnd = 0.75f,
            HitType = MissileHitType.AoE_Circle2Rect,
            MoveType = MissileMoveType.FollowsPlayer,
            spawnOffset = new Unity.Mathematics.float3(0, 0, -0.5f),
        }
    };
}