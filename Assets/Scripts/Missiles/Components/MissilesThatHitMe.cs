using Unity.Entities;

[InternalBufferCapacity(128)]
public struct MissilesThatHitMe : IBufferElementData {
    public Entity Missile;
    public ushort MissileRecycleCount;
    public float t;
    public float maxT;
    public HitEffect HitEffect;
    public bool skip;
}