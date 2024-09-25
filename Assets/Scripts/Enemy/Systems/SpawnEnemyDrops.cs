using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public partial class SpawnEnemyDrops : SystemBase {

    StageManager StageManager;

    protected override void OnCreate() {
        base.OnCreate();
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        UnityEngine.Debug.Assert(StageManager != null);
    }

    protected override void OnUpdate() {
        var q_pickups = new NativeQueue<float3>(Allocator.TempJob);
        var q_xpgems = new NativeQueue<float3>(Allocator.TempJob);
        var q_enemy_xp = new NativeQueue<float>(Allocator.TempJob);

        var table = PickUps.Table;
        var pickUpIDs = table.GetKeyArray(Allocator.TempJob);
        var playerLevel = SystemAPI.GetSingleton<Experience>().Level;
        var accumulator = StageManager.GetDropTableAccumulatedWeights();
        var weightRoll = UnityEngine.Random.Range(0, accumulator);
        var random = new Random((uint)UnityEngine.Random.Range(1, 100_000));
        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        var playerLuck = chara.CharacterStats.Get(Stats.LuckID).value;
        var xpgem_id = PickUps.XpGemID;

        Dependency = Entities
            .ForEach((EnemyAspect enemy) => {
            if(!enemy.State.ValueRO.isActive) return;
            if(!enemy.State.ValueRO.isDying) return;
            if(enemy.Drops.ValueRO._didDrop) return;
            enemy.Drops.ValueRW._didDrop = true;
            if(enemy.Drops.ValueRO.DropsPickups) {
                q_pickups.Enqueue(enemy.Transform.ValueRO.Position);
            }
            if(enemy.Drops.ValueRO.DropsXPGems) {
                q_xpgems.Enqueue(enemy.Transform.ValueRO.Position);
                q_enemy_xp.Enqueue(enemy.Enemy.ValueRO.XPGiven);
            }
        }).Schedule(Dependency);

        Dependency = Entities
            .WithNone<Treasure>()
            .WithReadOnly(table)
            .WithReadOnly(pickUpIDs)
            .WithDisposeOnCompletion(q_pickups)
            .WithDisposeOnCompletion(q_xpgems)
            .WithDisposeOnCompletion(q_enemy_xp)
            .WithDisposeOnCompletion(pickUpIDs)
            .ForEach((ref PickUp pickup, ref State state, ref LocalTransform ltf, ref Gfx gfx, ref ID id) => {
                if(state.isActive) return;
                var shouldDrop = false;
                var dropPosition = float3.zero;

                if(q_xpgems.TryDequeue(out dropPosition)) {
                    if(q_enemy_xp.TryDequeue(out var enemyXPGiven)) {
                        var rand = random.NextFloat(0f, 1f);
                        var xp = math.floor(rand + 0.5f * enemyXPGiven);
                        if(xp > 0) {
                            foreach(var ID in pickUpIDs) {
                                var arch = table[ID];
                                if(ID != xpgem_id.Guid) continue;
                                pickup = arch.pickUp;
                                pickup.value = xp;
                                shouldDrop = true;
                                id.Guid = ID;
                                gfx = arch.gfx;
                                break;
                            }
                        }
                    }
                }
                
                else if(q_pickups.TryDequeue(out dropPosition)) {
                    foreach(var ID in pickUpIDs) {
                        var arch = table[ID];
                        if(ID == xpgem_id.Guid) continue;
                        if(arch.pickUp.unlocksAt > playerLevel) continue;
                        if(arch.pickUp.rarity <= 0) continue;

                        var weightRoll = random.NextFloat(0, accumulator);

                        if(arch.pickUp._accumulatedWeight >= weightRoll) {
                            pickup = arch.pickUp;
                            shouldDrop = true;
                            id.Guid = ID;
                            gfx = arch.gfx;
                            //UnityEngine.Debug.Log($"Dropping pickup ID: {id.Guid}, {entry.rarity}, {entry.value}, {entry._accumulatedWeight}");
                            break;
                        }
                    }
                }

                if(shouldDrop) {
                    state.needsSpriteAndParticleSystem = true;
                    // NOTE: set the z to 0 because the enemies z will be a negative number since we are moving its collider out of the way when it dies
                    dropPosition.z = 0; // TODO: Assuming 0 is the "floor", perhaps this should be a configurable variable?
                    ltf.Position = dropPosition;
                    var scale = pickup.spriteScale;
                    if(scale == 0) {
                        scale = 1f;
                    }
                    ltf.Scale = scale;
                }
            }).Schedule(Dependency);
    }
}