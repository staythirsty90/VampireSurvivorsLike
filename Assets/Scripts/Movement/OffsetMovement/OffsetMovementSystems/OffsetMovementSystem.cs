using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(Init2), OrderLast = true)]
public partial class OffsetMovementSystem : SystemBase {
    protected override void OnUpdate() {
        var offsetMovement = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;

        Entities
            .ForEach((Entity e, ref LocalTransform position, in State state, in OffsetMovement movement) => {
                if(!state.isActive) return;

                switch(movement.OffsetType) {
                    case OffsetType.Default:
                        position.Position -= offsetMovement;
                        break;
                    case OffsetType.NoPlayerOffset:
                        break;
                    case OffsetType.NoPlayerOffsetX:
                        position.Position -= new float3(0, offsetMovement.y, 0);
                        break;
                    case OffsetType.NoPlayerOffsetY:
                        position.Position -= new float3(offsetMovement.x, 0, 0);
                        break;
                }
            }).ScheduleParallel();
    }
}