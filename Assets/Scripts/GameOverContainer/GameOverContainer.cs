using UnityEngine;
using Unity.Entities;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;
using Unity.Collections;

public class GameOverContainer : MonoBehaviour {
    public GameObject WeaponResultPrefab;
    public Image WeaponResultIconPrefab;

    private void Start() {
        gameObject.SetActive(false);
    }

    public void GameEvent_OnGameOver() {
        gameObject.SetActive(true);
        var damagetable = WeaponDamageTable.Table.AsReadOnly();

        var collision = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MissileCollisionSystem>();
        collision.Complete();

        var weaponsParent = GetComponentInChildren<HorizontalLayoutGroup>().transform; // NOTE: leverage this component to grab the transform

        var lootsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        var powerups = new NativeList<PowerUp>(Allocator.TempJob);
        lootsystem.GetAllPowerUps(ref powerups);

        foreach(var w in powerups) {
            if(!PowerUp.IsWeapon(w))
                continue;
            if(w.level == 0)
                continue;
            
            var elapsedTime = GameClockSystem.elapsedTime;
            var dmg = damagetable[w.weaponIndex];
            var timeActive = math.abs(elapsedTime - w._timeAquired);

            Debug.Log($"ElapsedTime: {elapsedTime}, TIMESTAMP: {GameClockSystem.GetTimeString(elapsedTime)}");
            Debug.Log($"TimeAcquired: {w._timeAquired}");

            var childIndex = 0;

            var icon = Instantiate(WeaponResultIconPrefab, weaponsParent.GetChild(childIndex++));
            icon.preserveAspect = true;
            icon.sprite = SpriteDB.Instance.Get(w.spriteName);

            var prefab = Instantiate(WeaponResultPrefab, weaponsParent.GetChild(childIndex++));
            var text = prefab.GetComponent<TextMeshProUGUI>();
            text.SetText($"{w.name}");

            prefab = Instantiate(WeaponResultPrefab, weaponsParent.GetChild(childIndex++));
            text = prefab.GetComponent<TextMeshProUGUI>();
            text.SetText(w.level == w.maxLevel ? "∞" : w.level.ToString());

            prefab = Instantiate(WeaponResultPrefab, weaponsParent.GetChild(childIndex++));
            text = prefab.GetComponent<TextMeshProUGUI>();
            text.SetText(dmg != 0 ? dmg.ToString("N0") : "—");

            prefab = Instantiate(WeaponResultPrefab, weaponsParent.GetChild(childIndex++));
            text = prefab.GetComponent<TextMeshProUGUI>();
            text.SetText($"{GameClockSystem.GetTimeString(timeActive)}");

            prefab = Instantiate(WeaponResultPrefab, weaponsParent.GetChild(childIndex++));
            text = prefab.GetComponent<TextMeshProUGUI>();
            if(dmg != 0 && timeActive != 0) {
                text.SetText($"{dmg / timeActive:0.0}");
            }
            else {
                text.SetText("—");
            }
            text.horizontalAlignment = HorizontalAlignmentOptions.Right;
        }
        powerups.Dispose();
    }
}