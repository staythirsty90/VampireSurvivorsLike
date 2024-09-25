using Unity.Entities;

public enum OffsetFlags {
    ParticleOffset      = 0 << 0,
    NoParticlesOffset   = 1 << 1,
}

public enum OffsetType : byte {
    Default,
    NoPlayerOffset,
    NoPlayerOffsetX,
    NoPlayerOffsetY,
}

public struct OffsetMovement : IComponentData {
    public OffsetType OffsetType;
    public OffsetFlags Flags;
}