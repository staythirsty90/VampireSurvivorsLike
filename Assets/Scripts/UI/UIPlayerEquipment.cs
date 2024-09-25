using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Entities;
using TMPro;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerEquipmentUI : SystemBase {
    List<Image> equipmentIcons = new();
    List<Image> equipmentIconsDarker = new();
    List<Transform> equipmentLevelBackgrounds = new();
    List<TextMeshProUGUI> equipmentLevelTexts = new();
    Transform background;
    PlayerStats PlayerStats;

    protected override void OnCreate() {
        PlayerStats = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerStats>();
        Debug.Assert(PlayerStats != null, "Couldn't find PlayerStats!");

        background = GameObject.Find("Player Equipment").transform.GetChild(0);
        Debug.Assert(background, "Couldn't find Player Equipment!");

        equipmentIcons = background.GetComponentsInChildren<Image>().Where(i => i.name == "Icon").ToList();
        Debug.Assert(equipmentIcons.Count > 0);

        equipmentIconsDarker = background.GetComponentsInChildren<Image>().Where(i => i.name == "Icon Darker").ToList();

        equipmentLevelBackgrounds = background.GetComponentsInChildren<Transform>().Where(i => i.name == "Level BG").ToList();
        equipmentLevelTexts = background.GetComponentsInChildren<TextMeshProUGUI>().Where(i => i.name == "Level Text").ToList();
    }

    void UpdateEquipment() {
        int top = 0;
        int bot = 6;

        for(int i = 0; i < equipmentIcons.Count; i++) {
            equipmentIcons[i].enabled = false;
            equipmentIconsDarker[i].enabled = false;
            equipmentLevelBackgrounds[i].gameObject.SetActive(false);
        }

        Entities.ForEach((ref PowerUpComponent puc) => {
            var powerup = puc.PowerUp;
            if(powerup.level == 0) {
                return;
            }
            var index = PowerUp.IsWeapon(powerup) ? top++ : bot++;
            if(index >= equipmentIcons.Count) {
                return;
            }
            var icon = equipmentIcons[index];
            var iconDarker = equipmentIconsDarker[index];
            var powerUpIcon = SpriteDB.Instance.Get(powerup.spriteName);

            // NOTE: Enable the icon graphic and set its sprite for each powerup the player has equipped.
            icon.enabled = true;
            icon.sprite = powerUpIcon;
            iconDarker.enabled = true;

            if(powerup.level > 1) {
                var levelBG = equipmentLevelBackgrounds[index];
                var levelText = equipmentLevelTexts[index];
                levelText.text = powerup.level.ToString();
                levelBG.gameObject.SetActive(true);
            }

        }).WithoutBurst().Run();
    }

    protected override void OnUpdate() {

        UpdateEquipment();

        return;
        var iconIndex = 0;
        Entities.ForEach((ref PowerUpComponent puc) => {
            var powerup = puc.PowerUp;
            if(powerup.Equals(default(PowerUp))) {
                return;
            }
            if(powerup.baseStats.Equals(default(GrowthStats))) {
                return;
            }
            if(powerup.level == 0) {
                return;
            }
            if(powerup.Weapons == null) {
                return;
            }
            var icon = equipmentIcons[iconIndex]; // we can't use "i" since we use continue, otherwise we will get the wrong icon (Image) from the list
            iconIndex++;

            var weapon = powerup.Weapons[0];
            var cooldown = 0; // PowerUp.GetCooldown(powerup, SystemAPI.GetSingleton<CharacterComponent>().character.CharacterStats.Get(Stats.CooldownReductionID).value);

            if(powerup.PowerUpType == PowerUpType.ChargedBuff) {
                icon.fillAmount = powerup.baseStats.Charges != 0 ? 1 : 1 - weapon._cooldownTimer / cooldown;
                return;
            }

            icon.fillAmount = 1 - (weapon._cooldownTimer / cooldown);
            //Debug.Log($"updating cooldown for {powerup.name}, cooldownTimer: {cooldownTimer}, cooldownBuffed: {cooldownBuffed}, fillAmount: {icon.fillAmount}, {icon.transform.parent.name}");

        }).WithoutBurst().Run();
    }
}