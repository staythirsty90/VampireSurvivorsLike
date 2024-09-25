using Unity.Entities;

public struct Treasure : IComponentData {
    public TreasureSettings treasureSettings;
    public float grabDistance;
}