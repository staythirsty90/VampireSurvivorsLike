using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GetActiveEnemies : SystemBase {
    public NativeArray<int> activeEnemies;
    public NativeArray<int> activeElites;
    public NativeArray<int> spawnedElites;
    public NativeArray<int> activeSwarmers;
    //public NativeList<int> spawnedSwarmers;
    public NativeArray<int> spawnedSwarmers;
    public NativeArray<int> activeWallers;
    public NativeArray<int> spawnedWallers;
    StageManager StageManager;
    protected override void OnCreate() {
        base.OnCreate();
        activeEnemies = new NativeArray<int>(1, Allocator.Persistent);
        activeElites = new NativeArray<int>(1, Allocator.Persistent);
        activeSwarmers = new NativeArray<int>(1, Allocator.Persistent);
        activeWallers = new NativeArray<int>(1, Allocator.Persistent);
        spawnedElites = new NativeArray<int>(1, Allocator.Persistent);
        spawnedSwarmers = new NativeArray<int>(1, Allocator.Persistent);
        //spawnedSwarmers = new NativeList<int>(300, Allocator.Persistent);
        spawnedWallers = new NativeArray<int>(1, Allocator.Persistent);
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        activeEnemies.Dispose();
        activeElites.Dispose();
        activeSwarmers.Dispose();
        activeWallers.Dispose();
        spawnedElites.Dispose();
        spawnedSwarmers.Dispose();
        spawnedWallers.Dispose();
    }

    public void Complete() {
        Dependency.Complete();
    }

    protected override void OnUpdate() {

        // TODO: Parallel writing to the containers eventually miscalculates spawned entities, why?

        var minute = StageManager.CurrentWave.minute;

        activeEnemies[0] = 0;
        activeElites[0] = 0;
        activeSwarmers[0] = 0;
        activeWallers[0] = 0;

        spawnedElites[0] = 0;
        spawnedSwarmers[0] = 0;
        //spawnedSwarmers.Clear();
        spawnedWallers[0] = 0;

        var active_enemies = activeEnemies;
        var active_elites = activeElites;
        var spawned_elites = spawnedElites;
        var active_swarmers = activeSwarmers;
        //NativeList<int>.ParallelWriter spawned_swarmers = spawnedSwarmers.AsParallelWriter();
        var spawned_swarmers = spawnedSwarmers;

        Entities
            .WithNone<Destructible>()
            .ForEach((in Enemy enemy, in State state, in SpriteFrameData sf_data) => {
                if(state.WillBeActivated(sf_data.needsRendererUpdated)) {

                    switch(enemy.specType) {
                        case Enemy.SpecType.Normal:
                            active_enemies[0]++;
                            break;
                        case Enemy.SpecType.Elite:
                            if(state.minuteOfSpawn == minute) {
                                spawned_elites[0]++;
                            }
                            break;

                        case Enemy.SpecType.Swarmer: {
                            if(state.minuteOfSpawn == minute) {
                                spawned_swarmers[0] += 1 + state.numberOfSpawnsThisWave;
                            }
                            if(state.WillBeActivated(sf_data.needsRendererUpdated)) {
                                active_swarmers[0]++;
                            }
                        }
                        break;
                    }
                }
            }).Schedule();
        
        var active_wallers = activeWallers;
        var spawned_wallers = spawnedWallers;

        Entities
            .WithNativeDisableParallelForRestriction(active_wallers)
            .WithNativeDisableParallelForRestriction(spawned_wallers)
            .ForEach((in Enemy enemy, in State state, in SpriteFrameData sf_data) => {
                if((enemy.Flags & EnemyFlags.Wall) == 0) return;
                if(state.minuteOfSpawn == minute) {
                    spawned_wallers[0]++;
                }
                if(state.WillBeActivated(sf_data.needsRendererUpdated)) {
                    active_wallers[0]++;
                }
            }).Schedule();
    }
}