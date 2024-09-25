using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public partial class StageManager : SystemBase {
    public Stage CurrentStage;
    public Stage.Wave CurrentWave;

    float DropTable_accumulatedWeights;
    int currentWaveIndex = -1;
    int previousMinute;
    uint previousPlayerLevel;

    public void UpdatePickupDropTable() {
        var PlayerExperience = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>();
        Debug.Assert(PlayerExperience != null);
        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        InitializePickupDropTable(SystemAPI.GetSingleton<Experience>().Level, chara.CharacterStats.Get(Stats.LuckID).value);
    }

    public void SetStage(Stage stage) {
        CurrentStage = stage;
        currentWaveIndex = 0;
        CurrentWave = CurrentStage.Waves[currentWaveIndex];
        Debug.Assert(CurrentWave != null);

        //foreach(var w in CurrentStage.Waves) {
        //    foreach(var we in w.WaveEvents) {
        //        foreach(var enemy in we.enemies) {
        //            if(Enemies.Table.TryGetValue(enemy.Guid, out EnemyArchetype ea)) {
        //                var fields = typeof(Enemies).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        //                foreach(var idField in fields) {
        //                    if( idField.Name.Contains("ID") && ((ID)idField.GetValue(null)).Guid == enemy.Guid) {
        //                        Debug.LogError($"{stage.stageName} has enemy ID: {idField.Name}");
        //                    }
        //                }
        //            }
        //            else {
        //                Debug.LogError($"couldnt get enemy {enemy.Guid} from table");
        //            }
        //        }
        //    }
        //}
    }

    void InitializePickupDropTable(uint level, float luck) {
        var t = PickUps.Table;
        Debug.Assert(t.IsCreated && t.Count != 0 && level != 0 && luck != 0, $"isCreated:{t.IsCreated}, count:{t.Count}, level:{level}, luck:{luck}");
        DropTable_accumulatedWeights = 0; // dont forget to reset this!

        var keys = t.GetKeyArray(Allocator.TempJob);
        var values = t.GetValueArray(Allocator.TempJob);
        Debug.Assert(keys.Length == values.Length);

        for(var i = 0; i < keys.Length; i++) {
            var id = keys[i];
            var arch = values[i];

            if(arch.pickUp.rarity <= 0) continue;
            if(arch.pickUp.unlocksAt > level) continue;

            if(arch.pickUp.usesLuck)
                DropTable_accumulatedWeights += arch.pickUp.rarity * luck;
            else
                DropTable_accumulatedWeights += arch.pickUp.rarity;

            arch.pickUp._accumulatedWeight = DropTable_accumulatedWeights;
            t[id] = arch;
        }

        keys.Dispose();
        values.Dispose();
    }

    public DestructibleSettings GetCurrentDestructibleSettings() {
        return CurrentStage.destructibleSettings;
    }

    public float GetDropTableAccumulatedWeights() {
        return DropTable_accumulatedWeights;
    }

    protected override void OnUpdate() {
        // Pickups that destructibles can drop "unlock" based on the player's current level.
        // So, we reinitialize the DropTable everytime the player levels up
        var playerLevel = SystemAPI.GetSingleton<Experience>().Level;
        if(previousPlayerLevel != playerLevel) {
            UpdatePickupDropTable();
            previousPlayerLevel = playerLevel;
        }

        var currentMinute = GameClockSystem.GetMinute();
        var delta = currentMinute - previousMinute;
        if(delta > 0) {
            //Debug.Log($"GetNextWave");
            
            if(CurrentStage == null) {
                Debug.Log($"CurrentStage is null");
                return;
            }
            
            if(CurrentStage.Waves.Length <= currentWaveIndex + 1) {
                Debug.Log($"CurrentStage Waves Length ({CurrentStage.Waves.Length}) <= currentWaveIndex + 1 ({currentWaveIndex + 1})");
                return;
            }

            var nextWave = CurrentStage.Waves[currentWaveIndex + 1];
            if(nextWave.minute != currentMinute) {
                Debug.Log($"NextWave minute ({nextWave.minute}) != currentMinute ({currentMinute})");
                return;
            }
            CurrentWave = nextWave;
            currentWaveIndex++;
            Debug.Log($"currentMinute: {currentMinute}, previousMinute: {previousMinute}, delta: {delta}");
            previousMinute = currentMinute;
        }
    }
}