using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial class LootSystem : SystemBase {
    NativeList<Entity> PowerUpEntities;
    NativeList<Entity> PowerUpStore;
    NativeList<FixedString64Bytes> EvolutionStore;
    NativeList<PowerUp> ExcludedPowerUps;

    List<Entity> _pickedPowerUps = new(4);

    float PowerUp_accumulatedWeight = 0f;
    readonly HashSet<int> picks = new();
    readonly HashSet<FixedString64Bytes> alreadyPickedPowerUps = new();
    PlayerExperience PlayerExperience;
    
    protected override void OnDestroy() {
        base.OnDestroy();
        PowerUpEntities.Dispose();
        PowerUpStore.Dispose();
        EvolutionStore.Dispose();
        ExcludedPowerUps.Dispose();
    }

    protected override void OnCreate() {
        base.OnCreate();
        PowerUpEntities     = new NativeList<Entity>(Allocator.Persistent);
        PowerUpStore        = new NativeList<Entity>(Allocator.Persistent);
        EvolutionStore      = new NativeList<FixedString64Bytes>(Allocator.Persistent);
        ExcludedPowerUps    = new NativeList<PowerUp>(Allocator.Persistent);
        PlayerExperience    = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>();
        Debug.Assert(PlayerExperience  != null);
    }

    public void GetAllPowerUps(ref NativeList<PowerUp> list) {
        var array = list;
        Entities.ForEach((in PowerUpComponent puc) => {
            array.Add(puc.PowerUp);
        }).Run();
        list = array;
    }

    protected override void OnUpdate() {
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();

        if(frameStats.powerUpNameToGiveToPlayer.Length > 0) {
            foreach(var index in frameStats.powerUpNameToGiveToPlayer) {
                GivePowerUp(index);
            }
            frameStats.powerUpNameToGiveToPlayer.Clear();
            SystemAPI.SetSingleton(frameStats);
        }
    }
    
    public void InitPowerUpStore() {
        PowerUpStore.Clear();
        PowerUp_accumulatedWeight = 0;

        Entities.ForEach((Entity e, ref PowerUpComponent puc) => {
            var powerUp = puc.PowerUp;
            if(powerUp.isEvolution || powerUp.rarity == 0) {
                ExcludedPowerUps.Add(powerUp);
            }
            else {
                for(int i = 0; i < powerUp.maxLevel; i++) {
                    PowerUpStore.Add(e);
                }
            }

            // Set powerup's weight.
            if(powerUp.rarity <= 0) return;
            PowerUp_accumulatedWeight += powerUp.rarity;
            powerUp._weight = PowerUp_accumulatedWeight;

            puc.PowerUp = powerUp;
            PowerUpEntities.Add(e);
        }).WithoutBurst().Run();
    }

    public void GivePowerUp(FixedString64Bytes name) {
        var time = UnityEngine.Time.time;
        var needsLevelUp = false;
        var AllPowerUps = GetComponentLookup<PowerUpComponent>();
        var names = new NativeList<FixedString64Bytes>(Allocator.TempJob);

        Entities.ForEach((Entity e, ref PowerUpComponent puc) => {
            var powerup = puc.PowerUp;
            
            if(powerup.name != name) {
                return;
            }

            // TODO: Improve this code for playing audio for powerups.
            if(!powerup.ParticleSystemGameObjectName.IsEmpty) {
                GameObject.Find(powerup.ParticleSystemGameObjectName.ToString()).GetComponent<AudioSource>().Play();
            }

            if(!PowerUp.IsPowerUpBonus(powerup)) {
                if(powerup.level > 0) {
                    //Debug.Log($"Had powerup ({powerup.name}), leveling it up.");
                    needsLevelUp = true;
                }
                else {
                    //Debug.Log($"Didn't have powerup ({powerup.name}), equipping it.");
                    powerup.level = 1; // NOTE: @IndexIssue
                    if(!powerup.isEvolution) {
                        RemovePowerUpFromStore(e);
                    }
                    else {
                        names.Add(powerup.name);
                        // Remove any "de-evolutions" from the Powerup Store and Equipment (The player may start with an Evolution Weapon so the PowerUp Store will have the 'de-evolutions'.
                        for(var i = PowerUpStore.Length - 1; i > -1; i--) {
                            var item = AllPowerUps[PowerUpStore[i]].PowerUp;
                            if(item.evolutionName == powerup.name) {
                                // Remove from Equipment first.
                                RemovePowerUp(ref item);
                                var devolvedpuc = AllPowerUps[PowerUpStore[i]];
                                devolvedpuc.PowerUp = item;
                                AllPowerUps[PowerUpStore[i]] = devolvedpuc;

                                PowerUpStore.RemoveAt(i);
                                Debug.Log($"removing de-evolution ({item.name}) because the character was given an evolved weapon ({powerup.name})");
                            }
                        }
                    }
                    powerup._timeAquired = time;
                }
            }
            else {
                Debug.Log($"PowerUp {powerup.name} is a bonus");
                switch(powerup.PowerUpEffect) {
                    case PowerUpEffect.HEAL_30:
                        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerHealSystem>().RequestHeal(30);
                        Debug.Log("Requesting Heal");
                        break;
                    case PowerUpEffect.COIN_50:
                        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GoldCollectedSystem>().RequestGold(50);
                        Debug.Log("Requesting 50 gold");
                        break;
                }
            }
            puc.PowerUp = powerup;
        }).WithoutBurst().Run();

        // TODO: Not happy about this complexity.
        for(var i = 0; i < names.Length; i++) {
            var devolvename = names[i];
            var index = EvolutionStore.IndexOf(devolvename);
            if(index != -1) {
                EvolutionStore.RemoveAt(index);
            }

            Entities.ForEach((Entity e, ref PowerUpComponent puc) => {
                var powerup = puc.PowerUp;
                if(powerup.evolutionName == devolvename) {
                    RemovePowerUp(ref powerup);
                }
                puc.PowerUp = powerup;
            }).WithoutBurst().Run();
        }
        names.Dispose();

        if(needsLevelUp) {
            LevelUpPowerUp(name);
        }
    }

    public void LevelUpPowerUp(FixedString64Bytes name) {
        Entities
            .ForEach((Entity e, ref PowerUpComponent puc) => {
                var powerup = puc.PowerUp;
            
                if(powerup.name != name) {
                    return;
                }
                if(powerup.level == powerup.maxLevel) {
                    // TODO: Maybe we do a limit break thing here?
                    Debug.LogWarning($"PowerUp {powerup.name} is already at max level!");
                    return;
                }

                if(powerup.growthStats.Length > 0) {
                    var index = Unity.Mathematics.math.max(0, powerup.level - 1); // @IndexIssue: growthStats of index 0 is for a Level 2 item, not a Level 1 item
                    if(index >= powerup.growthStats.Length) {
                        Debug.LogWarning($"growthStats has no more elements. did you set the maxlevel of the powerup? removing {powerup.name} from the powerups pool.");
                        RemovePowerUpFromStore(e);
                        return;
                    }
                    var gs = powerup.growthStats[index];
                    powerup.baseStats += gs;
                }

                powerup.level += 1;
                if(powerup.level == powerup.maxLevel) {
                    ExcludedPowerUps.Add(powerup);
                    if(!powerup.evolutionName.IsEmpty) {
                        EvolutionStore.Add(powerup.evolutionName);
                        Debug.LogWarning($"Adding PowerUp ({powerup.evolutionName}) into the Evolution Store.");
                    }
                }
                RemovePowerUpFromStore(e);
                puc.PowerUp = powerup;
        }).WithoutBurst().Run();
    }

    public PowerUp GetPowerUp(FixedString64Bytes name) {
        PowerUp powerup = default;
        Entities.ForEach((ref PowerUpComponent puc) => {
            if(puc.PowerUp.name == name) {
                powerup = puc.PowerUp;
            }
        }).Run();
        return powerup;
    }

    void RemovePowerUpFromStore(Entity exists) {
        int index = PowerUpStore.IndexOf(exists);
        if(index != -1) {
            PowerUpStore.RemoveAt(index);
        }
        else {
            Debug.LogError($"couldnt remove powerup entity ({exists})!, PowerUpStore Count: {PowerUpStore.Length}");
        }
    }
    
    public void GetLoot(ref List<Entity> pickedPowerUps, in TreasureSettings treasureSettings) {
        Debug.Assert(treasureSettings.level != 0, $"TreasureSettings level is {treasureSettings.level}!");
        var playerLevel = SystemAPI.GetSingleton<Experience>().Level;
        for(var i = 0; i < treasureSettings.level; i++) {
            var index = GetPowerUp(treasureSettings.prizeTypes[i], playerLevel);
            if(index == Entity.Null) {
                Debug.Log("couldn't find a powerup reward, giving coins instead");
                Entities.ForEach((Entity e, in PowerUpComponent puc) => {
                    if(index != Entity.Null) return; // Found a powerup already.
                    if(PowerUp.IsPowerUpBonus(puc.PowerUp)) {
                        index = e;
                        return;
                    }
                }).Run();
                Debug.Assert(index != Entity.Null);
            }
            pickedPowerUps.Add(index);
        }
    }

    Entity GetPowerUp(in PrizeType prizeType, uint playerLevel) {
        // TODO: Need to check the weights of the equipped items and roll for which to pull.
        var index = Entity.Null;
        
        var AllPowerups = GetComponentLookup<PowerUpComponent>();
        var powerupStore = PowerUpStore;
        var powerupEntities = PowerUpEntities;

        switch(prizeType) {
            case PrizeType.Evolution: {
                index = GetEvolution();
                if(index == Entity.Null) {
                    index = GetPowerUp(PrizeType.NewAny, playerLevel); // failed to get an evolution, try getting a random powerup instead
                }
            }
            break;

            case PrizeType.NewAny: {
                var r = Random.Range(0, 1f) * PowerUp_accumulatedWeight;
                Job
                    .WithReadOnly(AllPowerups)
                    .WithCode(() => {
                    
                        foreach(var entity in powerupStore) {
                            var powerup = AllPowerups[entity].PowerUp;
                            if(powerup.isEvolution) continue;
                            if(powerup._weight >= r) {
                                index = entity; 
                                break;
                            }
                        }

                }).Run();
            }
            break;
            
            case PrizeType.ExistingAny: {
                Job
                    .WithReadOnly(AllPowerups)
                    .WithCode(() => {
                        var weights = 0f;
                        foreach(var entity in powerupEntities) {
                            var pu = AllPowerups[entity].PowerUp;
                            if(pu.isEvolution) continue;
                            if(pu.level == 0) continue;
                            weights += pu._weight;
                        }
                        
                        var r = Random.Range(0, 1f) * weights;
                        foreach(var entity in powerupEntities) {
                            var pu = AllPowerups[entity].PowerUp;
                            if(pu.isEvolution) continue;
                            if(pu.level == 0) continue;
                            if(pu._weight >= r) {
                                index = entity;
                                break;
                            }
                        }
                    }).Run();
            }
            break;
            
            case PrizeType.NewWeapon: {
                var r = Random.Range(0, 1f) * PowerUp_accumulatedWeight;
                Job
                    .WithReadOnly(AllPowerups)
                    .WithCode(() => {
                        foreach(var entity in powerupStore) {
                            var pu = AllPowerups[entity].PowerUp;
                            if(!PowerUp.IsWeapon(pu)) continue;
                            if(pu.isEvolution) continue;
                            if(pu._weight >= r) {
                                index = entity;
                                break;
                            }
                        }
                }).Run();
            }
            break;

            case PrizeType.ExistingWeapon: {

                var ChanceForExistingPowerUp = 0.3f;
                if(Random.Range(0, 1f) >= ChanceForExistingPowerUp) {
                    return index;
                }

                picks.Clear();
                var tries = 0;

                while(picks.Count < PowerUpEntities.Length && tries < 100) { // TODO: Better algorithm that doesnt rely on "tries".
                    tries++;
                    var r = Random.Range(0, 1f) > 0.5 ? 0 : Random.Range(0, PowerUpEntities.Length);
                    var entity = PowerUpEntities[r];
                    var powerup = AllPowerups[entity].PowerUp;
                    if(!PowerUp.IsWeapon(powerup)) {
                        continue;
                    }
                    if(picks.Contains(r)) {
                        continue;
                    }
                    if(powerup.level == 0) {
                        continue;
                    }
                    if(powerup.level == powerup.maxLevel) {
                        continue;
                    }
                    if(2.5f * powerup.level + 1 < playerLevel) {
                        picks.Add(r);
                        index = entity;
                        break;
                    }
                }
            }
            break;

            case PrizeType.Random: {
                index = Random.Range(0, 1f) > 0.5f ? GetPowerUp(PrizeType.NewAny, playerLevel) : GetPowerUp(PrizeType.ExistingAny, playerLevel);
            }
            break;
        }

        return index;
    }

    Entity GetEvolution() {
        if(EvolutionStore.Length == 0) {
            return Entity.Null;
        }
        
        // TODO: Not happy about this complexity.
        var evolEntity = Entity.Null;
        for(var i = 0; i < EvolutionStore.Length; i++){
            var evolName = EvolutionStore[i];
            Entities
                .ForEach((Entity e, in PowerUpComponent puc) => {
                    if(puc.PowerUp.name == evolName && evolEntity == Entity.Null) {
                        evolEntity = e;
                        return;
                    }
            }).Run();
        }

        return evolEntity;
    }

    public void RemovePowerUp(ref PowerUp powerup) {
        
        if(powerup.Equals(default(PowerUp))) {
            Debug.LogWarning($"powerup ({powerup.name}) is already default!");
            return;
        }
        powerup.level = 0;

        // Reset some weapon data
        if(powerup.Weapons.Length > 0) {
            for(var i = 0; i < powerup.Weapons.Length; i++) {
                var weapon = powerup.Weapons[i];
                weapon._firedSoFar = 0;
                powerup.Weapons[i] = weapon;
            }
        }

        // TODO: Improve this code for playing audio for powerups.
        if(!powerup.ParticleSystemGameObjectName.IsEmpty) {
            GameObject.Find(powerup.ParticleSystemGameObjectName.ToString()).GetComponent<AudioSource>().Stop();
        }
    }
    
    public void GetLoot(ref List<Entity> pickedPowerUps, bool isBonus = false) {
        if(_pickedPowerUps.Count == 0) {
            GenerateLoot(isBonus);
        }
        foreach(var item in _pickedPowerUps) {
            pickedPowerUps.Add(item);
        }
        _pickedPowerUps.Clear();
    }

    void GetRegularLoot(ref List<Entity> pickedPowerUps) {
        var playerLuck  = SystemAPI.GetSingleton<CharacterComponent>().character.CharacterStats.Get(Stats.LuckID).value;
        var playerLevel = SystemAPI.GetSingleton<Experience>().Level;
        var roll = Random.Range(0f, 1f);
        var powerUpsToChoose = roll > 1 / playerLuck ? 4 : 3;
        //Debug.Log($"powerUpsToChoose: {powerUpsToChoose}, roll:{roll} luck:{luck}");

        // TODO improve the picking powerups algorithm. If powerUpsToChoose is greater than the remaining number of pickable powerups there will be an infinite loop.

        var WeaponCount = 0;
        var PassiveCount = 0;

        Entities.ForEach((in PowerUpComponent puc) => {
            var pu = puc.PowerUp;
            if(PowerUp.IsWeapon(pu) && pu.level > 0) {
                WeaponCount++;
            }
            else if(!PowerUp.IsWeapon(pu) && pu.level > 0) {
                PassiveCount++;
            }
        }).Run();


        alreadyPickedPowerUps.Clear();
        _pickedPowerUps.Clear();

        var AllPowerups = GetComponentLookup<PowerUpComponent>();

        var index = GetPowerUp(PrizeType.ExistingWeapon, playerLevel);
        //Debug.Log($"GetPowerUp ExistingWeapon, index: {index}");

        var powerUp = default(PowerUp);
        if(index != Entity.Null) {
            powerUp = AllPowerups[index].PowerUp;
        }

        if(!powerUp.Equals(default(PowerUp))) {
            if(powerUp.growthStats.Length == powerUp.level - 1) {
                Debug.LogWarning($"{powerUp.name}'s growthStats Count is equal to the item level. did you set the maxlevel of the weapon properly? skipping this weapon");
            }
            else {
                pickedPowerUps.Add(index);
                alreadyPickedPowerUps.Add(powerUp.name);
            }
        }
        int tries = 0;
        while(pickedPowerUps.Count < powerUpsToChoose && tries < 100) {
            tries++;
            if(playerLevel <= 3) {
                index = GetPowerUp(PrizeType.NewWeapon, playerLevel);
                //Debug.Log($"<= level 3, GetRandomWeighted_Weapon(), index: {index}");
            }
            else {
                index = GetPowerUp(PrizeType.NewAny, playerLevel);
                //Debug.Log($"GetRandomWeighted_WeaponOrPassive(), index: {index}");
            }
            
            if(index != Entity.Null) {
                powerUp = AllPowerups[index].PowerUp;
                if(alreadyPickedPowerUps.Contains(powerUp.name)) {
                    //Debug.Log($"Already picked {powerUp.name}, picking again...");
                    continue;
                }
                if(powerUp.growthStats.Length == powerUp.level - 1 && powerUp.level > 1) {
                    Debug.Log($"skipping? {powerUp.level}, picking again...");
                    continue;
                }

                if(PowerUp.IsWeapon(powerUp) && powerUp._timeAquired == 0 && WeaponCount == 6) {
                    Debug.Log($"Full on weapons... {powerUp.name} Level:{powerUp.level}, picking again...");
                    continue;
                }
                if(!PowerUp.IsWeapon(powerUp) && powerUp._timeAquired == 0 && PassiveCount == 6) {
                    Debug.Log($"Full on passives... {powerUp.name} Level:{powerUp.level}, picking again...");
                    continue;
                }
                if(powerUp.level >= powerUp.maxLevel) {
                    Debug.Log($"Max level... {powerUp.name} Level:{powerUp.level}, picking again...");
                    continue;
                }

                //Debug.Log($"Picking {powerUp.name}, Level:{powerUp.level}");
                pickedPowerUps.Add(index);
                alreadyPickedPowerUps.Add(powerUp.name);
            }
        }

        if(pickedPowerUps.Count == 0) {
            // Add bonus powerups since we didn't get any picks.
            Debug.Log("Add bonus powerups since we didn't get any picks");
            GetBonusLootAll(ref pickedPowerUps);
        }

        int pickedCount = pickedPowerUps.Count;
        Debug.Assert(pickedCount <= 4 && pickedCount != 0, pickedCount);
    }

    public void GenerateLoot(bool isBonus = false) {
        if(isBonus) {
            GetBonusLootAll(ref _pickedPowerUps);
        }
        else {
            GetRegularLoot(ref _pickedPowerUps);
        }
    }

    public void GetBonusLootAll(ref List<Entity> pickedPowerUps) {
        _pickedPowerUps.Clear();
        var picked = new NativeList<Entity>(Allocator.TempJob);
        Entities.ForEach((Entity e, in PowerUpComponent puc) => {
            if(PowerUp.IsPowerUpBonus(puc.PowerUp)) {
                picked.Add(e);
            }
        }).Run();
        foreach(var p in picked) {
            pickedPowerUps.Add(p);
        }
        picked.Dispose();
    }

    public void EmptyPowerUpStore() {
        PowerUpStore.Clear();
    }
}