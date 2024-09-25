using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine.UI;
using TMPro;

public struct Experience: IComponentData {
    public uint Level;
    public float LevelProgress;
    public float CurrentExperience;
    public float currentXPFactor;
    public float defaultXPFactor;
    public float _totalExpGained;
    public uint levelsGainedThisFrame;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerExperience : SystemBase {
    public bool waitingOnChoosePowerUp;
    Image ExpBar;
    TextMeshProUGUI PlayerLevelText;

    protected override void OnCreate() {
        base.OnCreate();
        PlayerLevelText = GameObject.Find("Player Level Text").GetComponent<TextMeshProUGUI>();
        Debug.Assert(PlayerLevelText != null);

        ExpBar = GameObject.Find("Bar").GetComponent<Image>();
        Debug.Assert(ExpBar != null);
        ExpBar.fillAmount = 0;

        CalculateXPFactors();
    }

    public void CalculateXPFactors() {
        Entities.ForEach((ref Experience exp) => {
            var level = exp.Level + exp.levelsGainedThisFrame;
            var defaultXP = exp.defaultXPFactor + 1.5f * math.floor(level / 20);
            defaultXP = math.min(defaultXP, 8f);
            exp.currentXPFactor = defaultXP * level * level;
        }).WithoutBurst().Run();
    }

    public void LevelUp(ref Experience exp) {
        var spillOver = exp.CurrentExperience;
        Debug.Assert(spillOver >= 0);
        exp.Level += 1;
        exp.levelsGainedThisFrame -= 1;
        if(exp.levelsGainedThisFrame < 0) exp.levelsGainedThisFrame = 0;
        exp.CurrentExperience = spillOver;
        exp.LevelProgress = exp.CurrentExperience / exp.currentXPFactor;
        waitingOnChoosePowerUp = true;
        PlayerLevelText.SetText($"LVL {exp.Level}");
        CalculateXPFactors();
    }

    public bool TryLevelUp() {
        var result = false;
        Entities.ForEach((ref Experience exp) => {
            if(exp.levelsGainedThisFrame > 0 && !waitingOnChoosePowerUp) {
                LevelUp(ref exp);
                result = true;
            }
        }).WithoutBurst().Run();
        return result;
    }

    protected override void OnUpdate() {
        if(!waitingOnChoosePowerUp) {
            var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
            if(frameStats.expGained == 0) {
                return;
            }

            Entities.ForEach((ref Experience exp) => {
                exp.CurrentExperience += frameStats.expGained;
                exp._totalExpGained += frameStats.expGained;
                while(exp.CurrentExperience >= exp.currentXPFactor) {
                    exp.CurrentExperience -= exp.currentXPFactor;
                    exp.levelsGainedThisFrame++;
                    CalculateXPFactors();
                }
                exp.LevelProgress = exp.CurrentExperience / exp.currentXPFactor;
                if(exp.levelsGainedThisFrame > 0) {
                    LevelUp(ref exp);
                }
            }).WithoutBurst().Run();
            frameStats.expGained = 0;
            SystemAPI.SetSingleton(frameStats);
        }
        
        Entities.ForEach((in Experience exp) => {
            if(exp.LevelProgress != ExpBar.fillAmount) {
                ExpBar.fillAmount = exp.LevelProgress;
            }
        }).WithoutBurst().Run();
    }

#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN
    public void DebugLevelUp(uint levelsToGain) {
        ExpBar.fillAmount = 1f;
        Entities.ForEach((ref Experience exp) => {
            exp.levelsGainedThisFrame = levelsToGain;
            LevelUp(ref exp);
        }).WithoutBurst().Run();
    }
#endif
}