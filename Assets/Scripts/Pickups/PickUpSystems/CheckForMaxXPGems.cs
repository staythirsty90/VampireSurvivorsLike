using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CheckForMaxXPGems : SystemBase {
    EntityLimits EntityLimits;
    StageManager StageManager;

    protected override void OnCreate() {
        base.OnCreate();
        EntityLimits = UnityEngine.Object.FindObjectOfType<EntityLimits>();
        UnityEngine.Debug.Assert(EntityLimits);

        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        UnityEngine.Debug.Assert(StageManager != null);
    }

    protected override void OnUpdate() {
        if(UnityEngine.Time.frameCount % 60 != 0) return;

        var stageSpawningType = StageManager.CurrentStage.spawningType;

        var maxGems = EntityLimits.XpGemsCount;
        var activeXpGems = new NativeArray<uint>(1, Allocator.TempJob);
        var xpgem_id = PickUps.XpGemID;
        var inner = ScreenBounds.InnerRect;
        var outer = ScreenBounds.OuterRect;

        Dependency = Entities.WithNativeDisableParallelForRestriction(activeXpGems).ForEach((in PickUp pickup, in ID id, in State state) => {
            if(id.Guid != xpgem_id.Guid) return;
            if(!state.isActive) return;
            activeXpGems[0] += 1;
        }).ScheduleParallel(Dependency);

        Dependency.Complete();
        //UnityEngine.Debug.Log($"activeGems: {activeXpGems[0]}");
        if(activeXpGems[0] >= maxGems) {
            var max = ScreenBounds.Max_XY;
            var spriteSize = new float3(2, 2, 2);
            var totalOutside = new NativeArray<uint>(1, Allocator.TempJob);
            var totalXpValue = new NativeArray<float>(1, Allocator.TempJob);
            var random = new Random((uint)UnityEngine.Random.Range(1, 100_100));

            Dependency = Entities
                .ForEach((ref Swoops swoops, ref LocalTransform ltf, in State state, in PickUp pickup, in ID id) => {
                    if(!state.isActive) return;
                    if(id.Guid != xpgem_id.Guid) return;
                    if(swoops.swooping) return;
                    if(swoops.grabbed) return;
                    if(ScreenBounds.IsInView(ltf.Position, spriteSize, max.x, max.y)) return;

                    totalOutside[0] += 1;
                    totalXpValue[0] += pickup.value;
                }).Schedule(Dependency);

            Dependency = Entities
                 .WithReadOnly(totalXpValue)
                 .WithDisposeOnCompletion(totalOutside)
                 .WithDisposeOnCompletion(totalXpValue)
                 .ForEach((ref PickUp pickup, ref Swoops swoops, ref LocalTransform ltf, ref State state, in ID id) => {
                     if(totalOutside[0] == 0) return;

                     if(!state.isActive) return;
                     if(id.Guid != xpgem_id.Guid) return;
                     if(swoops.swooping) return;
                     if(swoops.grabbed) return;
                     if(ScreenBounds.IsInView(ltf.Position, spriteSize, max.x, max.y)) return;

                     if(totalOutside[0] == 1) {
                         ltf.Position = ScreenBounds.GetPositionOutOfSight(stageSpawningType, inner, outer, ref random, spriteSize);
                         pickup.value = totalXpValue[0];
                         //UnityEngine.Debug.Log($"Combed XP Value: {pickup.value}");
                         state.needsSpriteAndParticleSystem = true;
                         totalOutside[0] = 0;
                         return;
                     }
                     totalOutside[0] -= 1;
                     ltf.Position = new float3(-500, 0, 0);
                     state.isActive = false;
                     pickup.value = 0;
                 }).Schedule(Dependency);
        }
        activeXpGems.Dispose();
    }
}