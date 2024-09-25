using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(Init1))]
public partial class SpawnEnemySystem : SystemBase {
    StageManager stagemanager;
    EntityLimits EntityLimits;
    GetActiveEnemies GetActiveEnemies;
    protected override void OnCreate() {
        base.OnCreate();
        stagemanager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        Debug.Assert(stagemanager != null);
        EntityLimits = UnityEngine.Object.FindObjectOfType<EntityLimits>();
        Debug.Assert(EntityLimits != null);
        GetActiveEnemies = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GetActiveEnemies>();
        Debug.Assert(GetActiveEnemies != null);
    }

    protected override void OnUpdate() {

        if(stagemanager.CurrentStage.Waves.Length == 0) return;
        
        var random          = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        var EnemyTable      = Enemies.Table;
        var spriteDatas     = SpriteDB.Instance.SpriteSizeHashMap;
        var stageSpawnType  = stagemanager.CurrentStage.spawningType;
        var inner           = ScreenBounds.InnerRect;
        var outer           = ScreenBounds.OuterRect;
        
        GetActiveEnemies.Complete();

        for(var i = 0; i < stagemanager.CurrentWave.WaveEvents.Count; i++) {
            var we = stagemanager.CurrentWave.WaveEvents[i];
            if(we == null) {
                continue;
            }
            var eventType = we.eventType;
            var minute = stagemanager.CurrentWave.minute;

            switch(eventType) {
                
                case WaveEventType.SpawnEnemies: {
                    #region CheckIfCanSpawnEnemies
                    var activeEnemiesCount = GetActiveEnemies.activeEnemies[0];
                    var minimumEnemiesWanted = we.amount;
                    var maxSpawns = minimumEnemiesWanted - activeEnemiesCount;

                    if(activeEnemiesCount >= minimumEnemiesWanted) {
                        //Debug.LogWarning($"active enemies ({activeEnemiesCount}) >= minimum wanted ({minimumEnemiesWanted})");
                        continue;
                    }

                    if(activeEnemiesCount >= we.amount) {
                        continue;
                    }

                    if(maxSpawns < 0) { // ????
                        continue;
                    }

                    if(we.enemies == null || we.enemies.Length == 0) {
                        //Debug.LogWarning($"found a SpawnEnemies event but its enemies array not setup! {ge.enemies}");
                        continue;
                    } 
                    #endregion

                    var enemiesToSpawn = new NativeList<ID>(Allocator.TempJob);
                    foreach(var enemy in we.enemies) {
                        enemiesToSpawn.Add(enemy);
                    }
                    var enemiesSpawnedSoFar = new NativeArray<int>(1, Allocator.TempJob);
                    enemiesSpawnedSoFar[0] = activeEnemiesCount;

                    var enemyIndex = 0;

                    Entities
                        .WithName("InitializeEnemies")
                        .WithNone<Destructible>()
                        .WithReadOnly(EnemyTable)
                        .WithReadOnly(enemiesToSpawn)
                        .WithReadOnly(spriteDatas)
                        .WithDisposeOnCompletion(enemiesToSpawn)
                        .WithDisposeOnCompletion(enemiesSpawnedSoFar)
                        .ForEach((EnemyAspect enemy, ref SpriteFrameData sf_data, ref SpawnData spawnData) => {
                            var id = enemiesToSpawn[enemyIndex];
                            var arch = EnemyTable[id.Guid];

                            if(CannotSpawn(enemy, arch, enemiesSpawnedSoFar, minimumEnemiesWanted, ref sf_data)) {
                                return;
                            }

                            SetData(in EnemyTable, id, enemy, minute);
                            enemyIndex += 1;
                            if(enemyIndex >= enemiesToSpawn.Length) {
                                enemyIndex = 0;
                            }
                            SetSpawnPosition(stageSpawnType, inner, outer, ref spawnData, in enemy.Gfx.ValueRO, in spriteDatas, ref random);
                            sf_data.needsRendererUpdated = true;
                            enemiesSpawnedSoFar[0]++;
                        }).Schedule();
                }
                break;

                case WaveEventType.SpawnElite: {
                    #region CheckIfCanSpawnElites
                    if(EntityLimits.ElitesCount == 0) continue;

                    if(GetActiveEnemies.spawnedElites[0] >= we.maximumSpawnsPerWave)
                        continue;

                    var minimumEnemiesWanted = we.amount;
                    var maxSpawns = minimumEnemiesWanted - GetActiveEnemies.activeElites[0];

                    var enemyID = we.enemies[0]; // TODO: Check for more than 0 index.
                    if(enemyID.Guid == Guid.Empty) {
                        Debug.LogWarning("found an event to spawn an elite but the enemy ID was an empty guid!");
                        continue;
                    } 
                    #endregion

                    var enemiesSpawned = new NativeArray<int>(1, Allocator.TempJob);

                    Entities
                        .WithName("InitializeElites")
                        .WithReadOnly(EnemyTable)
                        .WithDisposeOnCompletion(enemiesSpawned)
                        .ForEach((EnemyAspect enemy, ref SpawnData spawnData, ref SpriteFrameData sf_data) => {
                            var arch = EnemyTable[enemyID.Guid];
                            if(CannotSpawn(enemy, in arch, in enemiesSpawned, in maxSpawns, ref sf_data)) {
                                return;
                            }
                            SetData(in EnemyTable, enemyID, enemy, minute);
                            SetSpawnPosition(stageSpawnType, inner, outer, ref spawnData, in enemy.Gfx.ValueRO, in spriteDatas, ref random);
                            sf_data.needsRendererUpdated = true;
                            enemiesSpawned[0]++;
                        }).Schedule();
                }
                break;

                case WaveEventType.Swarm: {
                    if(EntityLimits.SwarmerCount == 0) continue;

                    var spawnedSwarmers = GetActiveEnemies.spawnedSwarmers[0];
                    //Debug.Log($"tick: {ge.tick}, delay: {ge.delay}, repeat: {ge.repeat}, spawned: {spawnedSwarmers}");

                    if(spawnedSwarmers != 0) {
                        we.tick += UnityEngine.Time.deltaTime;
                        if(we.tick < we.delay) {
                            continue;
                        }
                    }

                    var limit = math.min(we.amount * we.repeat, we.maximumSpawnsPerWave); // TODO: With this logic swarmers may continue to spawn.

                    if(spawnedSwarmers >= limit) {
                        Debug.Log($"swarmers limit reached: {limit}");
                        continue;
                    }
                    else {
                        //Debug.Log($"SpawnEnemy::spawning swarmers, spawned so far : {spawnedSwarmers}, limit: {limit}");
                    }

                    var currentSwarmerID = we.enemies[0]; // TODO: Check for more than 0 index.
                    if(currentSwarmerID.Guid == Guid.Empty) {
                        Debug.Log($"SpawnEnemy::swarmer guid is empty!");
                        continue;
                    }

                    var spawnPos = ScreenBounds.GetPositionOutOfSight(stageSpawnType, inner, outer, ref random, new float3(2, 2, 2));
                    var maxSpawns = we.amount;
                    var enemiesSpawned = new NativeArray<int>(1, Allocator.TempJob);

                    Entities
                        .WithName("InitializeSwarmers")
                        .WithReadOnly(EnemyTable)
                        .WithDisposeOnCompletion(enemiesSpawned)
                        .ForEach((EnemyAspect enemy, ref SpriteFrameData sf_data, ref SpawnData spawnData) => {
                            var arch = EnemyTable[currentSwarmerID.Guid];
                            if(CannotSpawn(enemy, in arch, in enemiesSpawned, in maxSpawns, ref sf_data)) {
                                return;
                            }
                            SetData(in EnemyTable, currentSwarmerID, enemy, minute);
                            var pos = random.NextFloat2Direction() * random.NextFloat(1f, 4f);
                            spawnData.spawnPosition.xy = spawnPos.xy + pos; // TODO: Adjust spawn position based on sprite size and if we are spawning on -x, +x, -y, +y.
                            enemy.Movement.ValueRW.Direction = math.normalize(float3.zero - spawnData.spawnPosition);
                            enemy.Movement.ValueRW.Direction.z = 0;
                            sf_data.needsRendererUpdated = true;
                            enemiesSpawned[0]++;
                        }).Schedule();
                }
                break;

                case WaveEventType.Wall: {
                    var spawnedWallers = GetActiveEnemies.spawnedWallers[0];

                    if(spawnedWallers >= we.amount) {
                        //Debug.Log($"swarmers limit reached: {limit}");
                        continue;
                    }

                    var amount = we.amount;
                    var wallPositions = new NativeArray<float3>(we.amount, Allocator.TempJob);
                    var angleIncrement = 360f / amount;
                    var ovalWidth = ScreenBounds.X_Max;
                    var ovalHeight = ScreenBounds.Y_Max;

                    Dependency = Job.WithCode(() => {
                        for(int i = 0; i < amount; i++) {
                            var angle = i * angleIncrement;
                            var x = math.cos(math.radians(angle)) * ovalWidth / 2f * 3f;
                            var y = math.sin(math.radians(angle)) * ovalHeight / 2f * 3f;
                            var spawnPosition = new float3(x, y, 0);
                            wallPositions[i] = spawnPosition;
                        }
                    }).Schedule(Dependency);

                    var timedLife = 60f;
                    var enemiesSpawned = new NativeArray<int>(1, Allocator.TempJob);
                    var enemyID = we.enemies[0]; // TODO: Check for more than 0 index.

                    Dependency = Entities
                        .WithName("InitializeWallEnemies")
                        .WithNone<Destructible>()
                        .WithReadOnly(EnemyTable)
                        .WithReadOnly(wallPositions)
                        .WithDisposeOnCompletion(wallPositions)
                        .WithDisposeOnCompletion(enemiesSpawned)
                        .ForEach((EnemyAspect enemy, ref SpawnData spawnData, ref SpriteFrameData sf_data) => {
                            var arch = EnemyTable[enemyID.Guid];
                            if(CannotSpawn(enemy, in arch, in enemiesSpawned, in amount, ref sf_data)) {
                                return;
                            }
                            SetData(in EnemyTable, enemyID, enemy, minute);
                            enemy.Enemy.ValueRW.Flags |= EnemyFlags.Wall | EnemyFlags.TimedLife;
                            spawnData.spawnPosition = wallPositions[enemiesSpawned[0]];
                            enemy.Enemy.ValueRW.timedLife = timedLife;
                            enemiesSpawned[0]++;
                            sf_data.needsRendererUpdated = true;
                        }).Schedule(Dependency);
                }
                break;

                case WaveEventType.KillEnemies: {
                    Entities.ForEach((ref Enemy enemy, in State state) => {
                        if(state.isActive) {
                            enemy.Health = 0;
                        }
                    }).Schedule();
                }
                break;
            }
        }

        // Find Enemy entities that are readyToActivate, Activate them and set their Positions.

        Entities
            .WithName("ActivateAndPlaceEnemies")
            .WithNone<Destructible>()
            .ForEach((EnemyAspect enemy, in SpawnData spawnData) => {
                if(enemy.State.ValueRO.isActive) return;
                if(!enemy.State.ValueRO.readyToActivate) return;
                enemy.Transform.ValueRW.Position = spawnData.spawnPosition;
                enemy.State.ValueRW.readyToActivate = false;
                enemy.State.ValueRW.isActive = true;
                enemy.State.ValueRW.angle = random.NextFloat(0,10);
            }).ScheduleParallel();

        // Cleanup Waves that run only Once
        // NOTE: There's no guarantee that the WaveEvent actually fired. For Example: perhaps there weren't any available Swarmer enemies to be spawned but we will still consume the event.
        for(var i = 0; i < stagemanager.CurrentWave.WaveEvents.Count; i++) {
            var ge = stagemanager.CurrentWave.WaveEvents[i];
            if(ge == null) {
                continue;
            }
            if(ge._repeatsLeft == 0) {
                stagemanager.CurrentWave.WaveEvents[i] = null;
                Debug.Log("Setting WaveEvent to null!");
            }
            else if(ge.repeat > -1 && ge.tick >= ge.delay) {
                ge.tick = 0;
                ge._repeatsLeft -= 1;
            }
        }
    }

    static bool CannotSpawn(EnemyAspect enemy, in EnemyArchetype arch, in NativeArray<int> enemiesSpawned, in int amount, ref SpriteFrameData sf_data, bool debug = false) {
        var state = enemy.State.ValueRO;
        if(debug) {
            Debug.Log($"state.isActive: {state.isActive}, (enemiesSpawned[0] >= amount): {enemiesSpawned[0] >= amount}, state.readyToActivate: {state.readyToActivate}, sf_data.needsRendererUpdated: {sf_data.needsRendererUpdated}, arch.enemy.specType != enemy.Enemy.ValueRO.specType: {arch.enemy.specType != enemy.Enemy.ValueRO.specType}");
        }
        return state.isActive ||
        (enemiesSpawned[0] >= amount) ||
        state.readyToActivate ||
        sf_data.needsRendererUpdated ||
        arch.enemy.specType != enemy.Enemy.ValueRO.specType;
    }

    public static void SetData(in NativeHashMap<Guid, EnemyArchetype> EnemyTable, in ID enemyID, EnemyAspect enemy, in sbyte minute) {
        enemy.Id.ValueRW = enemyID;
        var spawnedCount = enemy.State.ValueRO.numberOfSpawnsThisWave;
        if(minute == enemy.State.ValueRO.minuteOfSpawn) {
            spawnedCount++;
        }
        var arch = EnemyTable[enemy.Id.ValueRO.Guid];
        enemy.Enemy.ValueRW = arch.enemy;
        enemy.Enemy.ValueRW.Health = arch.enemy.MaxHealth;
        enemy.State.ValueRW.minuteOfSpawn = minute;
        enemy.State.ValueRW.numberOfSpawnsThisWave = spawnedCount;
        enemy.Enemy.ValueRW.AttackRange = (byte)(enemy.Enemy.ValueRO.AttackRange == 0 ? 1 : enemy.Enemy.ValueRO.AttackRange);
        enemy.Drops.ValueRW = arch.drops;
        
        var scale = new float3(arch.spriteScale.Value);
        enemy.SpriteScale.ValueRW.Value = math.any(scale) ? scale : new float3(1);

        enemy.Gfx.ValueRW = arch.gfx;

        enemy.OffsetMovement.ValueRW = arch.offsetMovement;

        enemy.Movement.ValueRW = arch.movement;
    }

    public static void SetSpawnPosition(StageSpawnType stageSpawnType, in Rect inner, in Rect outer, ref SpawnData spawnData, in Gfx gfx, in NativeHashMap<FixedString32Bytes, SpriteData> spriteDatas, ref Unity.Mathematics.Random random) {
        var name = gfx.spriteName;
        var spritesize = float3.zero;
        if(spriteDatas.ContainsKey(name)) {
            spritesize = spriteDatas[name].size;
        }
        else if(spriteDatas.ContainsKey($"{name}_{gfx.startingFrame}")) {
            spritesize = spriteDatas[$"{name}_{gfx.startingFrame}"].size;
        }
        var spawnpos = ScreenBounds.GetPositionOutOfSight(stageSpawnType, inner, outer, ref random, spritesize);
        spawnData.spawnPosition = spawnpos;
    }
}