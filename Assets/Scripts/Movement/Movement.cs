using Unity.Entities;
using Unity.Mathematics;

public enum MoveType : byte {
    Forward,
    Spiral,
    Orbit,
    Stationary,
    Boomerang,
    FollowsPlayer,
    Arc,
}

public struct Movement : IComponentData {
    public MoveType MoveType;
    public float3 Direction;
    public byte MoveSpeed;
}