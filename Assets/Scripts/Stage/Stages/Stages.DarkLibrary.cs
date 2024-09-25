using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static partial class Stages {
    public static Stage GetDarkLibrary() {
        //UnityEngine.Debug.Log("GetDarkLibrary()");
        var stage = new Stage() { 
            stageName = "Dark Library",
            description = "This is a Library, and it is dark.",
            iconName = "LibraryStageThumbnail",
            unlocked = true,
            tips = "Avoid the walls.",
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

            floorGameObject = Resources.Load<GameObject>("Floors/Library/Floor"),

            Rect = new Rect {
                size = new Vector3(ScreenBounds.OuterRect.width, ScreenBounds.OuterRect.height, 0),
                center = new Vector3(0, 0, 0),
            },

            spawningType = StageSpawnType.LeftAndRightSidesOnly,
            
            obstructibleSettings = new Stage.ObstructibleSettings {
                spriteName = "library",
            },

            configuration = new Stage.Configuration(),

            destructibleSettings = new DestructibleSettings {
                type = Enemies.CandelabraID,
                frequency = 1f,
                chance = 10,
                chanceMax = 50,
                maxDestructibles = 10,
            },

            Waves = new Stage.Wave[] {

                new Stage.Wave (
                    minute:   0,
                    waveEvents: new List<WaveEvent> {
                        //Stage.Event(WaveEventType.Swarm,        new []{ Enemies.TestSwarmID }, amount:20, delay:5f, repeat:3),
                        //Stage.Event(WaveEventType.Wall,         new []{ Enemies.TestPlantID }, amount:150),
                        Stage.Event(WaveEventType.SpawnEnemies, new []{ Enemies.TestPlantID  }, amount: 150, repeat:-1),
                    }
                ),
            },
        };

        stage.CreateObstructibles = () => World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntityMakerSystem>().MakeDarkLibrary(stage);
        return stage;
    }
}