using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct FrameStatsSingleton : IComponentData {
    public float totalHealed;
    public float expGained;
    public bool collectedMagnet;
    public float3 playerMoveDelta;
    public float3 previousPlayerMoveDelta;
    public float3 playerMoveTotal;
    public bool isPlayerFacingRight;
    public FixedList512Bytes<FixedString64Bytes> powerUpNameToGiveToPlayer;
    public FixedList512Bytes<StatIncrease> statIncreasesToGiveToPlayer;
    public bool collectedRosary;
}