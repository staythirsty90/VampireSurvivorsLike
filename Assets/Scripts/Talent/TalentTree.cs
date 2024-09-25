using Unity.Entities;

[InternalBufferCapacity(32)]
public struct TalentTreeComponent : IBufferElementData {
    public Talent talent;
}