using Unity.Entities;
using Unity.Collections;
using TMPro;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class GoldCollectedSystem : SystemBase {
    public NativeArray<uint> GoldCollectedThisFrame;
    
    TextMeshProUGUI GoldCollectedText;

    float _previousGold;

    PlayerGold PlayerGold;

    int requestedGold;

    protected override void OnCreate() {
        base.OnStartRunning();
        GoldCollectedThisFrame = new NativeArray<uint>(1, Allocator.Persistent);
    
        PlayerGold = Object.FindObjectOfType<PlayerGold>();
        Debug.Assert(PlayerGold != null);

        var go = GameObject.Find("GoldCollected Text");
        Debug.Assert(go != null);

        GoldCollectedText = go.GetComponent<TextMeshProUGUI>(); // UNDONE
        Debug.Assert(GoldCollectedText != null);
        GoldCollectedText.SetText(PlayerGold.Gold.ToString("N0"));
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        GoldCollectedThisFrame.Dispose();
    }

    public void RequestGold(int amount) {
        requestedGold = amount;
    }

    protected override void OnUpdate() {
        var Gold = PlayerGold.Gold;
        var greed = SystemAPI.GetSingleton<CharacterComponent>().character.CharacterStats.Get(Stats.GreedID).value;

        if(GoldCollectedThisFrame[0] > 0 || requestedGold != 0) {
            Gold += (GoldCollectedThisFrame[0] + requestedGold) * (1 + (1 * greed));
            GoldCollectedText.SetText(Gold.ToString("N0"));
            GoldCollectedThisFrame[0] = 0;
            requestedGold = 0;
            _previousGold = Gold;
        }

        if(_previousGold != Gold) {
            GoldCollectedText.SetText(Gold.ToString("N0"));
            _previousGold = Gold;
        }
        PlayerGold.Gold = Gold;
    }
}