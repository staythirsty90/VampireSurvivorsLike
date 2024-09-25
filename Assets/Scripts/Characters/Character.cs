using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;

public struct CharacterComponent : IComponentData {
    public Character character;
}

public struct CharacterStats {
    public NativeHashMap<Guid, Stat> stats;
    public NativeHashMap<Guid, float> baseStats;
    public StatIncreaseOnLevel statIncreaseOnLevel;

    public Stat Get(Guid stat) {
        if(stat == Guid.Empty) {
            throw new Exception("Get::Stat guid was empty!");
        }

        if(!stats.IsCreated) {
            Init();
        }
        return stats[stat];
    }
    public StatIncreaseOnLevel MakeLevelStat(byte level, byte maxAmount, Guid statType, float value, bool isPercent = false) {
        return new() {
            levelInterval = level,
            maxApplications = maxAmount,
            StatIncrease = new() {
                statType = statType,
                value = value,
                isPercentageBased = isPercent,
            }
        };
    }

    public void Init() {
        stats = new NativeHashMap<Guid, Stat>(32, Allocator.Persistent);
        baseStats = new NativeHashMap<Guid, float>(32, Allocator.Persistent);
        statIncreaseOnLevel = new StatIncreaseOnLevel();
        foreach(var s in Stats.StatTable) {
            var copy = (Stat)s.Value.Invoke(null, null);
            stats.Add(s.Key, copy);
            baseStats.Add(s.Key, copy.value);
        }
        statIncreaseOnLevel = MakeLevelStat(5, 5, Stats.MightID, 10, true);
    }
}

public struct TalentTreeLink : IComponentData {
    public Entity talentTreeEntity;
}

public struct PowerUpBuffer: IBufferElementData {
    public Entity powerupEntity;
}

[Serializable]
public struct Character {
    public CharacterStats CharacterStats;
    public FixedList512Bytes<FixedString64Bytes> startingWeapons;
    public FixedString32Bytes charName;
    public FixedString512Bytes desc;
    public FixedList512Bytes<FixedString32Bytes> idleSpriteNames;
    public FixedList512Bytes<FixedString32Bytes> runSpriteNames;
    public float spriteScale;
    
    public void SetStat(Guid statType, float value) {
        
        if(statType == Guid.Empty) {
            Debug.LogWarning("SetStat::Stat guid was empty!");
            return;
        }
        if(!CharacterStats.stats.IsCreated) {
            CharacterStats.Init();
        }

        var stat = CharacterStats.Get(statType);
        stat.value = value;
        CharacterStats.stats[statType] = stat;
        CharacterStats.baseStats[statType] = value;
        if(statType.Equals(Stats.MaxHealthID)) {
            SetStat(Stats.HealthID, value);
        }
    }

    public static string GenerateStatDescription(StatIncreaseOnLevel statIncreaseOnLevel) {
        var statIncrease = statIncreaseOnLevel.StatIncrease;
        var levelInterval = statIncreaseOnLevel.levelInterval;
        var max = statIncreaseOnLevel.maxApplications;

        if(statIncrease.statType == Guid.Empty) {
            Debug.LogWarning("GenerateStatDescription::statIncrease.statType guid is empty!");
            return "GenerateStatDescription::statIncrease.statType guid is empty!";
        }

        var isPercentageBased = statIncrease.isPercentageBased ? "%" : "";
        var description = $"Gains {statIncrease.value}{isPercentageBased} {Stats.StatTable[statIncrease.statType].Name} every {levelInterval} Levels";

        description += $" (max {max * statIncrease.value}{isPercentageBased})";

        return description;
    }
}