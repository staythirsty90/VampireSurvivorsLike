using Unity.Entities;

public struct Obstructible : IComponentData {
    [System.Flags]
    public enum Flags : byte {
        None        = 1 << 0,
        DoNotWrapX  = 1 << 1,
        DoNotWrapY  = 1 << 2,
    }

    public Flags flags;
}

public struct ObstructibleBlockerTag : IComponentData { }