using Unity.Entities;

[System.Serializable]
public struct DestructibleSettings {
    public ID type;
    public float frequency;
    public int chance;
    public int chanceMax;
    public int maxDestructibles;
}

public struct Destructible : IComponentData {}