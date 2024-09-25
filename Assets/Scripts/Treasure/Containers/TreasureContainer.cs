using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

public class TreasureContainer : MonoBehaviour {
    public TreasureSettings TreasureSettings;
    public TreasureUpdateSystem treasureSystem;

    Animator TreasureAnimator;
    GameObject TreasureOpenButton;
    GameObject TreasureCloseButton;

    Image RewardImage;
    Image RewardEffects;
    TextMeshProUGUI RewardInfo;

    PlayerStats PlayerStats;
    LootSystem LootSystem;

    List<Entity> rewardEntities = new(4);

    private void Awake() {
        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null);

        PlayerStats = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerStats>();
        Debug.Assert(PlayerStats != null);

        TreasureAnimator = GameObject.Find("Treasure UI").GetComponent<Animator>();
        Debug.Assert(TreasureAnimator != null);

        TreasureOpenButton = GameObject.Find("Treasure Open Button");
        Debug.Assert(TreasureOpenButton != null);
        TreasureOpenButton.GetComponent<Button>().onClick.AddListener(() => { OnClickedOpen(); });

        TreasureCloseButton = GameObject.Find("Treasure Close Button");
        Debug.Assert(TreasureCloseButton != null);
        TreasureCloseButton.GetComponent<Button>().onClick.AddListener(() => { OnClickedClose(); });

        TreasureCloseButton.SetActive(false);

        RewardImage = GameObject.Find("Reward").GetComponent<Image>();
        Debug.Assert(RewardImage != null);

        RewardEffects = GameObject.Find("Effects").GetComponent<Image>();
        Debug.Assert(RewardEffects != null);

        RewardInfo = GameObject.Find("Reward Info").GetComponent<TextMeshProUGUI>();
        Debug.Assert(RewardInfo != null);

        RewardImage.gameObject.SetActive(false);
        RewardEffects.gameObject.SetActive(false);
        RewardInfo.gameObject.SetActive(false);

        Hide();
    }

    public void Show() {
        gameObject.SetActive(true);
        TreasureOpenButton.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public bool IsShowing() {
        return gameObject.activeInHierarchy;
    }

    public void OnClickedOpen() {
        TreasureAnimator.SetTrigger("Open");
        TreasureOpenButton.SetActive(false);
        TreasureCloseButton.SetActive(true);

        RewardImage.gameObject.SetActive(true);
        RewardEffects.gameObject.SetActive(true);
        RewardInfo.gameObject.SetActive(true);

        // TODO: Play open treasure animation.
        var lootsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        lootsystem.GetLoot(ref rewardEntities, TreasureSettings);
        Debug.Assert(rewardEntities != null && rewardEntities.Count > 0);

        var index = rewardEntities[0];
        var AllPowerups = lootsystem.GetComponentLookup<PowerUpComponent>(true);
        var reward = AllPowerups[index].PowerUp;

        RewardImage.sprite = SpriteDB.Instance.Get(reward.spriteName);
        RewardImage.preserveAspect = true;
        
        string text = $"{reward.name}";
        
        if(!PowerUp.IsPowerUpBonus(reward)) {
            if(!reward.Equals(default(PowerUp))) {
                text += $" Level {reward.level + 1}\n{PowerUp.GetDescription(reward)}";
            }
            else {
                Debug.Log($"reward level is {reward.level}");
                text += $" Level {reward.level + 1}\n{PowerUp.GetDescription(reward)}";
            }
        }
        else {
            text += $"\n{reward.description}";
        }

        RewardInfo.SetText(text);
        LootSystem.GivePowerUp(reward.name);
    }

    public void OnClickedClose() {
        TreasureCloseButton.SetActive(false);
        RewardImage.gameObject.SetActive(false);
        RewardEffects.gameObject.SetActive(false);
        RewardInfo.gameObject.SetActive(false);
        //
        // Reset the collected treasure.
        //
        treasureSystem.collectedTreasure[0] = false;
        rewardEntities.Clear();
        TreasureSettings = default;
    }
}

public partial class TreasureUpdateSystem : SystemBase {
    public TreasureContainer TreasureContainer;
    public NativeArray<bool> collectedTreasure;
    NativeArray<TreasureSettings> TreasureSettings;

    protected override void OnCreate() {
        base.OnCreate();
        collectedTreasure = new NativeArray<bool>(1, Allocator.Persistent);
        TreasureSettings = new NativeArray<TreasureSettings>(1, Allocator.Persistent);
        TreasureContainer = GameObject.FindObjectOfType<TreasureContainer>(true);
        Debug.Assert(TreasureContainer);
        TreasureContainer.treasureSystem = this;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        collectedTreasure.Dispose();
        TreasureSettings.Dispose();
    }

    protected override void OnUpdate() {
        if(collectedTreasure[0]) {
            TreasureContainer.TreasureSettings = TreasureSettings[0];
            Debug.Log($"Collected Treasure! {TreasureContainer.TreasureSettings.level}");
            return;
        }

        var playerPos = float3.zero;
        var collected = collectedTreasure;
        var treasureSettings = TreasureSettings;
        var playerMoveDelta = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;

        if(playerMoveDelta.x == 0 && playerMoveDelta.y == 0) {
            return;
        }

        Entities.ForEach((ref LocalTransform position, ref State state, in Treasure treasure ) => {
            if(!state.isActive) return;
            if(collected[0]) return; // Prevent collecting only one chest if multiple chests are stacked on top of each other.

            var distance = math.distance(position.Position, playerPos);
            if(treasure.grabDistance >= distance) {
                collected[0] = true;
                position.Position = new float3(-500, 0, 0);
                state.isActive = false;
                treasureSettings[0] = treasure.treasureSettings;
                return;
            }
        }).Schedule();
    }
}