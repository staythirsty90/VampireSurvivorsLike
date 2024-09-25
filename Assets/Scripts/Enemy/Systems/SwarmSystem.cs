using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SwarmSystem : SystemBase {
    protected override void OnUpdate() {
        var bounds = new float2(ScreenBounds.X_Max + 5, ScreenBounds.Y_Max + 5);
        var time = UnityEngine.Time.time;

        Entities
            .ForEach((EnemyAspect enemy, ref PhysicsVelocity velocity) => {
                if(!enemy.State.ValueRO.isActive) return;
                if(enemy.Enemy.ValueRO.specType != Enemy.SpecType.Swarmer) return;
                if(enemy.State.ValueRO.isDying) {
                    velocity.Linear = float3.zero;
                    enemy.Transform.ValueRW.Position.z = 100;
                    return;
                }
                if(Utils.IsLocked(enemy.State.ValueRO.StatusEffects)) {
                    velocity.Linear = float3.zero;
                    return;
                }

                var d = enemy.Movement.ValueRO.Direction * enemy.Enemy.ValueRO.MoveSpeed;
                velocity.Linear = d;

                enemy.Transform.ValueRW.Rotation = new quaternion(quaternion.identity.value.x, quaternion.identity.value.y, quaternion.RotateZ(math.sin(time * 5f) * 0.1f).value.z, quaternion.identity.value.w);

                var pos = enemy.Transform.ValueRO.Position;
                if(pos.x > bounds.x || pos.x < -bounds.x || pos.y > bounds.y || pos.y < -bounds.y) {
                    enemy.State.ValueRW.doRecycle = true;
                }
                enemy.Transform.ValueRW.Position.z = 0;
            }).ScheduleParallel();
    }
}