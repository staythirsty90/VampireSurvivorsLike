using Unity.Collections;
using Unity.Entities;
[System.Serializable]
public struct Gfx: IComponentData {
    public FixedString32Bytes spriteName;
    public byte startingFrame;
}