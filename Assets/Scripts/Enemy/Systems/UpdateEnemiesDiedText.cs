using TMPro;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UpdateEnemiesDiedText : SystemBase {
    int EnemiesKilled = 0;
    TextMeshProUGUI EnemiesKilledText;

    protected override void OnCreate() {
        base.OnCreate();
        EnemiesKilledText = GameObject.Find("EnemiesKilled Text").GetComponent<TextMeshProUGUI>();
        Debug.Assert(EnemiesKilledText != null);
        EnemiesKilledText.SetText(EnemiesKilled.ToString());
    }
    protected override void OnUpdate() {

        var dyingEnemies = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EnemySystem>().DyingEnemies;

        var length = dyingEnemies.Length;
        if(length > 0) {
            EnemiesKilled += length;
            EnemiesKilledText.SetText(EnemiesKilled.ToString("N0"));
            dyingEnemies.Clear();
        }
    }
}