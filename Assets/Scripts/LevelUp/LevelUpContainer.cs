using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpContainer : MonoBehaviour {
    public List<Entity> PickedPowerUps => pickedPowerUps;
    List<Entity> pickedPowerUps = new();

    LootSystem LootSystem;
    ChoosePowerUp[] choosePUs = new ChoosePowerUp[4];

    private void Awake() {
        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null);

        choosePUs = transform.GetComponentsInChildren<ChoosePowerUp>();
        Debug.Assert(choosePUs[0] && choosePUs[1] && choosePUs[2] && choosePUs[3] != null);

        foreach(var b in transform.GetComponentsInChildren<Button>()) {
            if(b.name == "Reroll Button") {
                b.onClick.AddListener(() => { RerollChoices(); });
            }
            else if (b.name == "Skip Button") {
                b.onClick.AddListener(() => { SkipChoices(); });
            }
        }
    }

    private void Start() {
        gameObject.SetActive(false);
    }

    public bool IsShowing() {
        return gameObject.activeInHierarchy;
    }

    public void Show() {
        gameObject.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public void GiveRewards(bool isBonus = false) {
        pickedPowerUps.Clear();

        LootSystem.GetLoot(ref pickedPowerUps, isBonus);

        var pickedCount = pickedPowerUps.Count;
        Debug.Assert(pickedCount <= 4 && pickedCount != 0, pickedCount);
        var AllPowerups = LootSystem.GetComponentLookup<PowerUpComponent>(true);

        // TODO: Localize the strings for the text.
        var name = new FixedString64Bytes();
        var desc = new FixedString128Bytes();
        var spriteName = new FixedString32Bytes();
        var badge = new FixedString32Bytes();

        for(var i = 0; i < 4; i++) {
            if(i < pickedCount) {
                var pickedPowerUp = AllPowerups[pickedPowerUps[i]].PowerUp;
                name = pickedPowerUp.name;
                spriteName = pickedPowerUp.spriteName;
                if(PowerUp.IsPowerUpBonus(pickedPowerUp)) {
                    desc = pickedPowerUp.description;
                }
                else {
                    //Debug.Log($"checking for existing powerup ({pickedPowerUp.name})");
                    if(pickedPowerUp.level > 0) {
                        badge = $"Level {pickedPowerUp.level + 1}";
                        desc = PowerUp.GetDescription(pickedPowerUp);
                        //Debug.Log($"found existing powerup ({pickedPowerUp.name})");
                    }
                    else {
                        badge = "<color=#FFD806>New!";
                        desc = PowerUp.GetDescription(pickedPowerUp);
                        //Debug.Log($"powerup is new ({pickedPowerUp.name})");
                    }
                }
                choosePUs[i].SetData(i, spriteName.ToString(), name.ToString(), desc.ToString(), badge.ToString());
                choosePUs[i].gameObject.SetActive(true);
            }
            else {
                choosePUs[i].SetData(default);
                choosePUs[i].gameObject.SetActive(!isBonus);
            }
        }
    }

    public void RerollChoices() {
        GiveRewards();
    }

    public void SkipChoices() {
        pickedPowerUps.Clear();
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerExperience>().waitingOnChoosePowerUp = false;
    }
    
    void Update() {
        foreach(var choose in choosePUs) { // TODO: Assert that only one choosePU has IsClicked set to true?
            if(choose.IsClicked) {
                choose.IsClicked = false;
                var AllPowerups = LootSystem.GetComponentLookup<PowerUpComponent>(true);
                var pickedPowerUp = AllPowerups[pickedPowerUps[choose.Index]].PowerUp;
                LootSystem.GivePowerUp(pickedPowerUp.name);
                SkipChoices();
            }
        }
    }
}