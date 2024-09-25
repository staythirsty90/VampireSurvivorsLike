using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ResetEnemiesSystem : SystemBase {
    EndSimulationEntityCommandBufferSystem endSimulation;

    protected override void OnCreate() {
        base.OnCreate();
        endSimulation = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate() {
        var emptyEnt = Entity.Null;
        var ecb = endSimulation.CreateCommandBuffer().AsParallelWriter();

        Dependency = Entities.ForEach((Entity entity, int entityInQueryIndex, ref Enemy enemy, ref LocalTransform ltf, ref State state, ref Drops drops, ref DynamicBuffer<MissilesThatHitMe> buffer) => {
            if(!state.doRecycle) return;
            enemy.Reset();
            state.Reset();
            drops.Reset();
            if(SystemAPI.HasComponent<PhysicsVelocity>(entity)) {
                ecb.SetComponent(entityInQueryIndex, entity, new PhysicsVelocity());
            }
            ltf.Scale = 1f;
            var newX = enemy.specType == Enemy.SpecType.Swarmer ? -540 : -530;
            ltf.Position= new float3(newX, -2 * entity.Index, 0);

            var bufferArray = buffer.ToNativeArray(Unity.Collections.Allocator.Temp);
            var length = bufferArray.Length;
            for(int i = 0; i < length; i++) {
                var b = bufferArray[i];
                b.Missile = emptyEnt;
                b.t = 0;
                buffer[i] = b;
            }
            //UnityEngine.Debug.Log($"resetting enemy: {entity}");
        }).ScheduleParallel(Dependency);
        endSimulation.AddJobHandleForProducer(Dependency);
    }
}