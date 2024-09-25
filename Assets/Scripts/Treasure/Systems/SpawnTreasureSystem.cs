using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using mathRandom = Unity.Mathematics.Random;
using UnityRandom = UnityEngine.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SpawnTreasureSystem : SystemBase {
    
    protected override void OnUpdate() {
        var q_treasures = new NativeQueue<float3>(Allocator.TempJob);
        var random = new mathRandom((uint)UnityRandom.Range(1, 100_000));
        var playerLuck = SystemAPI.GetSingleton<CharacterComponent>().character.CharacterStats.Get(Stats.LuckID).value;
        
        Entities.ForEach((Entity e, ref Drops drops, in State state, in LocalTransform position) => {
            if(!state.isActive) return;
            if(!state.isDying) return;
            if(!drops.DropsTreasure || drops._droppedTreasure) {
                //UnityEngine.Debug.Log($"enemy already droppedTreasure: {enemy._droppedTreasure}");
                return;
            }
            drops._droppedTreasure = true;
            q_treasures.Enqueue(position.Position);
            //UnityEngine.Debug.Log($"queue up treasure: {q_treasures.Count}, {position.Value}");
            //UnityEngine.Debug.Log($"queue up elite: {q_elites.Count}, treasureSettings level: {elite.treasureSettings.level}");
        }).Run();

        Entities
            .WithDisposeOnCompletion(q_treasures)
            .ForEach((ref Treasure treasure, ref State state, ref LocalTransform ltf) => {
                if(state.isActive) return;
                if(!q_treasures.TryDequeue(out var dropPosition)) {
                    //UnityEngine.Debug.Log($"failed to dequeue treasure: {q_treasures.Count}");
                    return;
                }
                // TODO: Maybe we don't want a default treasuresettings for every drop.
                var treasureSettings = TreasureSettings.Create();
                if(treasureSettings.level == 0) {
                    //UnityEngine.Debug.Log($"TreasureSettings has level of 0! {treasureSettings.level}");
                    return;
                }
                var roll = random.NextInt(0, 101);
                var treasureLevel =
                  roll <= treasureSettings.chances[0] * playerLuck ? 3
                : roll <= treasureSettings.chances[1] * playerLuck ? 2
                : roll <= treasureSettings.chances[2] * playerLuck ? 1
                : 0;
                
                if(treasureLevel > 0) {
                    state.isActive = true;
                    // NOTE: Set the z to 0 because the enemies z will be a negative number since we are moving its collider out of the way when it dies.
                    dropPosition.z = 0; // TODO: Assuming 0 is the "floor", this should be a configurable variable.
                    ltf.Position = dropPosition;
                    treasure.treasureSettings = treasureSettings;
                    UnityEngine.Debug.Log("Spawned treasure!");
                }
                else {
                    //UnityEngine.Debug.Log($"Failed to spawn treasure. roll: {roll}");
                }
            }).Run();
    }
}