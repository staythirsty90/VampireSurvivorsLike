using System.Collections.Generic;
using UnityEngine;

namespace System.Runtime.CompilerServices {
    internal static class IsExternalInit { }
}
public enum WaveEventType : byte {
    NONE,
    SpawnEnemies,
    SpawnElite,
    Swarm,
    Wall,
    KillEnemies,
}

[System.Serializable]
public class WaveEvent {
    public WaveEventType eventType;
    public float delay;
    public sbyte repeat;
    public sbyte _repeatsLeft;
    public float frequency;
    public int amount;
    public ID[] enemies;
    public float tick;
    public int maximumSpawnsPerWave;
}

[System.Serializable]
public class Stage {
    
    [System.Serializable]
    public class Configuration {
        public float EnemySpeed = 0.2309f;
        public float GoldMultiplier = 1;
        public float PlayerPxSpeed = 0.825f;
        public float MissileSpeed = 1.65f;
    }

    [System.Serializable]
    public class Wave {
        public sbyte minute;
        public List<WaveEvent> WaveEvents;

        public Wave(sbyte minute, List<WaveEvent> waveEvents = default) {
            this.minute = minute;
            WaveEvents = waveEvents;
        }
    }

    [System.Serializable]
    public struct HyperSettings {
        public bool unlocked;
        public float PlayerPxSpeed;
        public float EnemySpeed;
        public float ProjectileSpeed;
        public float GoldMultiplier;
        public float EnemyMinimumMul;
        public float StartingSpawns;
        public string tips;
    }

    [System.Serializable]
    public struct ObstructibleSettings {
        public string spriteName;
    }

    public string stageName;
    public string description;
    public string iconName;
    public bool unlocked;
    public string tips;
    public HyperSettings hyper;
    public DestructibleSettings destructibleSettings;
    public ObstructibleSettings obstructibleSettings;
    public Wave[] Waves;
    public Configuration configuration;
    public GameObject floorGameObject;
    public Rect Rect;
    public StageSpawnType spawningType;
    public System.Action CreateObstructibles;

    public static WaveEvent Event(WaveEventType type, ID[] enemies, ushort amount = 100, sbyte repeat = 0, ushort maximum = 10_000, float delay = 0) {
        return new() {
            eventType = type,
            enemies = enemies,
            amount = amount,
            delay = delay,
            repeat = repeat,
            _repeatsLeft = repeat,
            maximumSpawnsPerWave = maximum,
        };
    }
}