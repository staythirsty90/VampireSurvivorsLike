using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using mathRandom = Unity.Mathematics.Random;
using UnityRandom = UnityEngine.Random;

[UpdateInGroup(typeof(Init1))]
public partial class SpawnDestructibleSystem : SystemBase {
    float t = 0f;
    StageManager StageManager;

    protected override void OnCreate() {
        base.OnCreate();
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        Debug.Assert(StageManager != null);
    }
    protected override void OnUpdate() {
        var dt = UnityEngine.Time.deltaTime;
        var destructibleSettings = StageManager.GetCurrentDestructibleSettings();
        var stageSpawnType = StageManager.CurrentStage.spawningType;
        var inner = ScreenBounds.InnerRect;
        var outer = ScreenBounds.OuterRect;

        if(t < destructibleSettings.frequency) {
            t += dt;
            //Debug.Log(destructibleSettings.frequency);
            return;
        }
        t = 0f;

        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        var playerLuck = chara.CharacterStats.Get(Stats.LuckID).value;

        //
        // Roll for a chance to spawn a destructible
        //
        var roll = UnityRandom.Range(0, 101) >= math.min(destructibleSettings.chance * playerLuck, destructibleSettings.chanceMax);
        if(!roll) {
            return;
        }
        //Debug.Log($"spawnDestructible roll: {roll}");

        var enemyTable = Enemies.Table;
        var random = new mathRandom((uint)UnityRandom.Range(1, 100_000));
        var found = new NativeArray<bool>(1, Allocator.TempJob);
        found[0] = false;

        var id = destructibleSettings.type;
        var guid = id.Guid;
        
        //
        // Search for an in-active destructible entity.
        // If we find one we spawn it.
        // NOTE: We probably don't need to keep setting the sprite and particlesystem since we only spawn one type
        // of destructible per 'run'. We can just set them once.
        //

        Entities
            .WithAll<Destructible>()
            .WithDisposeOnCompletion(found)
            .ForEach((SpriteRenderer sr, ParticleSystem ps, EnemyAspect enemy, ref SpriteFrameData sf_data) => {
                if(found[0]) return;
                if(enemy.State.ValueRO.isActive) return;
                found[0] = true;
                enemy.State.ValueRW.isActive = true;
                enemy.State.ValueRW.isDying = false;
                SpawnEnemySystem.SetData(in enemyTable, id, enemy, 0);
                (sr.sprite, sf_data.frameCount) = SpriteDB.Instance.Get(guid);
                sf_data.currentFrame = 0;
                sf_data.loopCount = 0;
                ParticleSystemHelper.SetParticleSystem(id.Guid, ps);
                var spriteSize = sr.sprite.bounds.size;
                var spawnPos = ScreenBounds.GetPositionOutOfSight(stageSpawnType, inner, outer, ref random, spriteSize * 1.25f);
                enemy.Transform.ValueRW.Position = spawnPos;
                //UnityEngine.Debug.Log($"spawnDestructible: spawned inactive destructible");
            }).WithoutBurst().Run();
    }
}