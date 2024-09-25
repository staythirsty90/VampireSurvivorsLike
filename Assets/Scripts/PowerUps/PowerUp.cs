using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Unity.Profiling;

[Serializable]
public struct GrowthStats {
    public float Cooldown;
    public float Amount;
    public float Interval;
    public float Area;
    public float Speed;
    public float Damage;
    public float Duration;
    public float Piercing;
    public float Charges;
    public float FreezeTime;
    
    public static GrowthStats operator +(GrowthStats a, GrowthStats b) {
        return new GrowthStats {
            Cooldown    = a.Cooldown    + b.Cooldown,
            Amount      = a.Amount      + b.Amount,
            Interval    = a.Interval    + b.Interval,
            Area        = a.Area        + b.Area,
            Speed       = a.Speed       + b.Speed,
            Damage      = a.Damage      + b.Damage,
            Duration    = a.Duration    + b.Duration,
            Piercing    = a.Piercing    + b.Piercing,
            Charges     = a.Charges     + b.Charges,
            FreezeTime  = a.FreezeTime  + b.FreezeTime,
        };
    }

    public static bool IsDefault(GrowthStats a) {
        return
        a.Cooldown      == 0 &&
        a.Amount        == 0 &&
        a.Interval      == 0 &&
        a.Area          == 0 &&
        a.Speed         == 0 &&
        a.Damage        == 0 &&
        a.Duration      == 0 &&
        a.Piercing      == 0 &&
        a.Charges       == 0 &&
        a.FreezeTime    == 0;
    }
}

public enum PowerUpEffect : byte {
    NONE,
    HEAL_30,
    COIN_50,
}

public enum PowerUpType : byte {
    ShootsMissile,
    ChargedBuff,
    Bonus,
}

public enum ShootType: byte {
    None,
    FiresOnlyOnceEver,
    ActivatesOnPlayerHit,
    BatchedMissiles,
    Interval,
}

[InternalBufferCapacity(128)]
public struct WeaponMissileEntity : IBufferElementData {
    public Entity MissileEntity;
}

[Serializable]
public struct MissileData {
    public ID missileID;
    public float3 spawnPosition;
    public SpawnPositionType SpawnPositionType;
    public SpawnVariationType SpawnVariationType;
    public Directions SpawnDirection;
    public MissileFlags additionalMissileFlags;
    public MissileFlags removeMissileFlags;
}

[Serializable]
public struct Weapon {
    public float _intervalTick;
    public ushort _firedSoFar;
    public ushort _firedTotal;
    public float _cooldownTimer;
    public float delay;
    public GrowthStats baseStats;
    public float3 targetDirection;
    public float3 spawnOffset;
    public ShootType PowerUpShootType;
    public MissileData missileData;

    public static bool IsDefault(Weapon w) {
        return w.missileData.missileID.Guid == Guid.Empty;
    }
    
    public void Init(float3 spawnOffset) {
        Init();
        
        if(math.any(spawnOffset)) {
            this.spawnOffset = spawnOffset;
        }
    }
    
    public void Init() {
        spawnOffset = new float3(-1, -1, -1);
    }
}

[InternalBufferCapacity(6)]
public struct WeaponComponent : IBufferElementData {
    public Weapon Weapon;
}

public static class PowerUpDescriptionTable {
    public static readonly Dictionary<Guid, FixedString512Bytes> DescriptionTable = new() {
        { Stats.HealthID             , "+{0} to Health\n"},
        { Stats.MaxHealthID          , "+{0}% to Maximum Health\n"},
        { Stats.MightID              , "+{0} to Missile Damage\n"},
        { Stats.MovementSpeedID      , "+{0}% Movement Speed\n"},
        { Stats.ArmorID              , "+{0} to Armor\n"},
        { Stats.LuckID               , "+{0}% to Luck\n"},
        { Stats.GreedID              , "Increases the value of Gold by {0}%\n"},
        { Stats.CooldownReductionID  , "+{0}% to Cooldown Reduction\n"},
        { Stats.AreaID               , "+{0}% to Missile Size\n"},
        { Stats.SpeedID              , "+{0}% Missile Speed\n"},
        { Stats.DurationID           , "+{0}% to Missile Duration\n"},
        { Stats.AmountID             , "+{0} Amount to All Missiles\n"},
        { Stats.PickUpGrabSpeedID    , "+{0} to Pickup Movement Speed\n"},
        { Stats.PickUpGrabRangeID    , "+{0} to Pickup Grab Range\n"},
        { Stats.GrowthID             , "+{0}% to XP gained\n"},
        { Stats.HealthRegenerationID , "Recover {0} Health per second\n"},
    };
}

public struct PowerUpComponent : IComponentData {
    public PowerUp PowerUp;
}

[Serializable]
public struct PowerUp : IComponentData {
    public sbyte weaponIndex;
    public FixedString64Bytes name;
    public FixedString64Bytes evolutionName;
    public FixedString64Bytes requiresName;
    public PowerUpType PowerUpType;
    public FixedString32Bytes spriteName;
    public FixedString64Bytes description;
    public bool _isWeapon;
    public byte maxLevel;
    public byte rarity;
    public byte level;
    public bool isEvolution;
    public PowerUpEffect PowerUpEffect;
    public FixedString64Bytes ParticleSystemGameObjectName;
    public FixedList512Bytes<StatIncrease> affectedStats;
    public GrowthStats baseStats;
    public FixedList512Bytes<GrowthStats> growthStats;
    public float _durationTimer;
    public float _weight;
    public float _timeAquired;
    public FixedList4096Bytes<Weapon> Weapons;
    public Entity familiarEntity;
    public Entity familiarTargetEntity;
    public bool familiarTargetFlag;
    public bool familiarSpawnedFlag;
    
    private static FixedString512Bytes stringbuffer = "";
    static readonly ProfilerMarker marker = new ("FixedFormat");
    
    public static void UpdateTimers(in PowerUp powerup, ref Weapon weapon, float playerCooldownReduction, in float dt) {
        if(powerup.PowerUpType == PowerUpType.ShootsMissile) {
            if(weapon._cooldownTimer > 0) { // _cooldownTimer starts at 0.
                weapon._cooldownTimer -= dt;
                if(weapon._cooldownTimer < 0) {
                    weapon._cooldownTimer = 0;
                }
            }
        }
        
        // Update the cooldown timers if the players cooldown reduction stat increases.
        var cooldown = GetCooldown(powerup, playerCooldownReduction);
        if(weapon._cooldownTimer > cooldown) {
            weapon._cooldownTimer = cooldown;
        }
        
        if(weapon.delay > 0) { // One time delay.
            weapon.delay -= dt;
            if(weapon.delay < 0) {
                weapon.delay = 0;
            }
        }
    }
    
    public static float GetCooldown(PowerUp powerup, float playerCooldownReduction) {
        var cooldown = powerup.baseStats.Cooldown;
        var cooldownBuffed = cooldown -  playerCooldownReduction * cooldown;
        return cooldownBuffed;
    }
    
    public static bool IsPowerUpBonus(PowerUp pu) {
        return pu.PowerUpType == PowerUpType.Bonus;
    }
    
    public static bool IsWeapon(PowerUp pu) {
        return pu._isWeapon;
    }
    
    public static string GetDescription(PowerUp pu) {
        stringbuffer.Clear();
        Debug.Assert(pu.level >= 0);
        //Debug.Log($"Getting desc, powerup level: {pu.level}");
        
        if(!pu.description.IsEmpty) { 
            if(pu.level == 0) {
                return pu.description.ToString();
            }
        }
        
        if(pu.growthStats.Length > 0) {
            var gsIdx = pu.level <= 0 ? 0 : pu.level - 1;
            var gs = pu.growthStats[gsIdx];
            // TODO: How can we automate this? Everytime we add a new Stat we have to manually set its description.
            SetDesc(gs.Amount     ,"+# to Amount"                          ); 
            SetDesc(gs.Cooldown   ,"Reduce Cooldown by #s"                 );
            SetDesc(gs.Interval   ,"Reduce Interval by #s"                 );
            SetDesc(gs.Area       ,"Increase Radius by #"                  );
            SetDesc(gs.Damage     ,"Increase Damage by #"                  );
            SetDesc(gs.Piercing   ,"Increase Piercing by #"                );
            SetDesc(gs.Speed      ,"Increase Speed by #%",             true);
            SetDesc(gs.Duration   ,"Increase Duration by #%",          true);
            SetDesc(gs.FreezeTime ,"Increase Duration of Freeze by #s"     );
        }
        
        else if(pu.affectedStats.Length > 0) {
            foreach(var s in pu.affectedStats) {
                var st = PowerUpDescriptionTable.DescriptionTable[s.statType];
                FixedString512Bytes v = $"{s.value}";
                stringbuffer.AppendFormat(in st, v);
            }
        }
        return stringbuffer.ToString();
    }
    
    static void SetDesc(float statvalue, string s, bool multiply = false) {
        marker.Begin();
        if(statvalue != 0) { stringbuffer += $"{s.Replace("#", multiply ? (statvalue * 100).ToString() : statvalue.ToString())}\n"; }
        marker.End();
    }
}