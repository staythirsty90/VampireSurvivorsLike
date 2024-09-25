using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public static partial class Stages {
    public static Stage GetForestStage() {
        //UnityEngine.Debug.Log("GetForestStage()");
        var stage = new Stage() {
            stageName = "Frantic Forest",
            description = "The forest is the only way to reach the Castle.\n\nThere's free cheese, though.",
            iconName = "ForestStageThumbnail",
            unlocked = true,
            tips = string.Empty,
            hyper = new Stage.HyperSettings {
                unlocked = false,
                PlayerPxSpeed = 1.75f,
                EnemySpeed = 1.75f,
                ProjectileSpeed = 1.25f,
                GoldMultiplier = 1.5f,
                EnemyMinimumMul = 1.25f,
                StartingSpawns = 20f,
                tips = "Gold multiplier: x1.5",
            },

            floorGameObject = Resources.Load<GameObject>("Floors/Forest/Floor"),

            Rect = new Rect {
                size = new Vector3(ScreenBounds.OuterRect.width, ScreenBounds.OuterRect.height, 0),
                center = new Vector3(0, 0, 0),
            },

            spawningType = StageSpawnType.AllFourSides,

            obstructibleSettings = new Stage.ObstructibleSettings {
                spriteName = "tree",
            },

            configuration = new Stage.Configuration(),

            destructibleSettings = new DestructibleSettings {
                type = Enemies.TorchID,
                frequency = 1f,
                chance = 10,
                chanceMax = 50,
                maxDestructibles = 10,
            },

            Waves = new Stage.Wave[] {
                
                new Stage.Wave (
                    minute:   0,
                    waveEvents: new List<WaveEvent> {
                        Stage.Event(WaveEventType.Swarm,        new []{ Enemies.TestSwarmID }, amount:50, delay:2f, repeat:33),
                        //Stage.Event(WaveEventType.Wall,         new []{ Enemies.TestPlantID }, amount:150, repeat:1),
                        Stage.Event(WaveEventType.SpawnEnemies, new []{ Enemies.SkeletonID, Enemies.TestBatID  }, amount:5000, repeat:-1),
                        Stage.Event(WaveEventType.SpawnElite,   new []{ Enemies.TestBossID  }, amount:1, repeat:-1, maximum:1, delay:0),
                        //    new TreasureSettings {
                        //        chances = new() { 0, 0, 30 },
                        //        prizeTypes = new(){ PrizeType.Evolution, PrizeType.Random, PrizeType.Random, PrizeType.Random, PrizeType.Random },
                        //        level = 1 }
                        //),
                    }
                ),

                new Stage.Wave (
                    minute:   1,
                    waveEvents: new List < WaveEvent > {
                        //Stage.Event(WaveEventType.Swarm,        new []{ Enemies.TestSwarmID }, amount:20, delay:5f, repeat:3),
                        Stage.Event(WaveEventType.SpawnEnemies, new []{ Enemies.TestBatID  }, amount:100, repeat:-1),
                    }
                ),

                new Stage.Wave (
                    minute:   2,
                    waveEvents: new List<WaveEvent> {
                        Stage.Event(WaveEventType.Swarm,        new []{ Enemies.TestSwarmID }, amount:20, delay:5f, repeat:3),
                        //Stage.Event(WaveEventType.SpawnEnemies, new []{ Enemies.SkeletonID  }),
                    }
                ),
            },
        };
        
        stage.CreateObstructibles = () => World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntityMakerSystem>().MakeForest(stage);
        return stage;
    }
}