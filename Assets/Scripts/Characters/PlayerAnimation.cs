using Unity.Collections;
using Unity.Entities;

public struct PlayerAnimation : IComponentData {
    public FixedList512Bytes<FixedString32Bytes> SpriteNames_Moving;
    public FixedList512Bytes<FixedString32Bytes> SpriteNames_Idle;
}