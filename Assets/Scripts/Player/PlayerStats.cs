using Unity.Mathematics;
using System;
using Unity.Entities;
using Unity.Collections;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerStats : SystemBase{

    protected override void OnUpdate() {

        var playerLevel = SystemAPI.GetSingleton<Experience>().Level;
        var AllPowerups = GetComponentLookup<PowerUpComponent>();
        var AllTalentTrees = GetBufferLookup<TalentTreeComponent>();
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var statsFromPickups = frameStats.statIncreasesToGiveToPlayer;
        frameStats.statIncreasesToGiveToPlayer.Clear();
        SystemAPI.SetSingleton(frameStats);

        Entities
            .WithReadOnly(AllPowerups)
            .WithReadOnly(AllTalentTrees)
            .ForEach((ref CharacterComponent cc, in DynamicBuffer<PowerUpBuffer> pub, in TalentTreeLink link ) => {
                var Stats = cc.character.CharacterStats.stats;
                var BaseStats = cc.character.CharacterStats.baseStats;
                var LevelStats = cc.character.CharacterStats.statIncreaseOnLevel;

                // Increase BaseStats from possible PickUp stat.
                foreach(var si in statsFromPickups) {
                    var value = 1f;
                    if(si.isPercentageBased) {
                        var min = math.max(1, value);
                        value = min * si.value * 0.01f;
                    }
                    else {
                        value = si.value;
                    }
                    BaseStats[si.statType] += value;
                }

                // Reset stats to BaseStats except for those with skipRecalc.
                foreach(var kvp in Stats) {
                    if(!kvp.Value.skipRecalc) {
                        kvp.Value.value = BaseStats[kvp.Key];
                    }
                }

                // Recalculate Talents.
                var tc = AllTalentTrees[link.talentTreeEntity];
                for(var i = 0; i < tc.Length; i++) {
                    var t = tc[i].talent;
                    var si = t.StatIncrease;
                    IncreaseStat(ref Stats, si, t.CurrentRank);
                }

                // Recalculate PowerUp Stats.
                for(var j = 0; j < pub.Length; j++) {
                    var ent = pub[j].powerupEntity;
                    var pu = AllPowerups[ent].PowerUp;
                    for(var k = 0; k < pu.affectedStats.Length; k++) {
                        IncreaseStat(ref Stats, pu.affectedStats[k], pu.level);
                    }
                }

                var increase             = math.min(LevelStats.maxApplications, math.floor(playerLevel / LevelStats.levelInterval));
                var statType             = LevelStats.StatIncrease.statType;
                var totalIncrease        = LevelStats.StatIncrease.value * increase;
                var isPercentageBased    = LevelStats.StatIncrease.isPercentageBased;

                var combinedStatIncrease = new StatIncrease {
                    statType = statType,
                    value = totalIncrease,
                    isPercentageBased    = isPercentageBased,
                };

                IncreaseStat(ref Stats, combinedStatIncrease);
                cc.character.CharacterStats.stats = Stats;

        }).WithoutBurst().Run();
        //Debug.Log($"Increasing stat {statType} by {(combinedStatIncrease.isPercentageBased ? totalIncrease + "%" : totalIncrease)}, increase was {increase}");
    }

    public static void IncreaseStat(ref NativeHashMap<Guid, Stat> stats, StatIncrease si, byte pulevel = 1) {
        if(pulevel == 0) {
            return;
        }

        var stat = stats[si.statType];
        if(si.isPercentageBased) {
            var min = math.max(1, stat.value);
            stat.value += min * si.value * 0.01f * pulevel;
        }
        else {
            stat.value += si.value * pulevel;
        }
        stats[si.statType] = stat;
    }
}