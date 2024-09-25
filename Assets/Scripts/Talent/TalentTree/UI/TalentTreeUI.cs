using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;

public class TalentTreeUI : MonoBehaviour {
    public TalentUI UITalentPrefab;
    readonly List<TalentUI> uiTalents = new(32);
    TextMeshProUGUI goldValueText;
    PlayerGold PlayerGold;
    bool _needsRedraw = false;
    
    NativeArray<Entity> _AllEntities; // @Temp
    EntityManager _EntityManager; // @Temp
    
    void Awake() {
        Debug.Assert(UITalentPrefab != null);

        goldValueText = transform.Find("UIGold").GetChild(2).GetComponent<TextMeshProUGUI>();
        Debug.Assert(goldValueText != null);

        PlayerGold = FindObjectOfType<PlayerGold>();
        Debug.Assert(PlayerGold != null);

        foreach(var b in transform.GetComponentsInChildren<Button>()) {
            if(b.name == "Close Button") {
                b.onClick.AddListener(() => { Hide(); });
                break;
            }
        }
    }

    void Start() {
        _needsRedraw = true;
        Hide();
    }

    private void OnDestroy() {
        if(_AllEntities.IsCreated) {
            _AllEntities.Dispose();
        }
    }

    void TryToBuyTalent(ref Talent talent, int index) {
        //Debug.Log($"PlayerGold: {PlayerGold.Gold}, talent cost: {talent.Cost}");

        if(PlayerGold.Gold < talent.Cost) {
            Debug.LogWarning($"Couldn't afford talent: {talent.Name}");
            return;
        }

        var cost_cached = talent.Cost;
        if(!Talent.RankUp(ref talent)) {
            Debug.LogWarning($"Couldn't rank up talent: {talent.Name}");
            return;
        }

        // TODO: Overly complicated.
        foreach(var e in _AllEntities) {
            if(_EntityManager.HasBuffer<TalentTreeComponent>(e)) {
                var buffer = _EntityManager.GetBuffer<TalentTreeComponent>(e);
                var tc = buffer[index];
                tc.talent = talent;
                buffer[index] = tc;
            }
        }
        PlayerGold.Gold -= cost_cached;
        _needsRedraw = true;
    }

    void LateUpdate() {

        if(Input.GetKeyDown(KeyCode.Escape)) {
            Hide();
        }

        { // debug
            if(Input.GetKeyDown(KeyCode.Alpha6)) {
                PlayerGold.Gold += 1000;
                goldValueText.SetText(PlayerGold.Gold.ToString());
                _needsRedraw = true;
            }
        }

        if(_needsRedraw) {
            
            var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();
     
            if(uiTalents.Count == 0) {
                var parent = transform.GetChild(2);
                // TODO: Overly complicated.
                _EntityManager = sys.EntityManager;
                _AllEntities = _EntityManager.GetAllEntities(Allocator.Persistent);
                var foundTalents = false;
                foreach(var e in _AllEntities) {
                    if(_EntityManager.HasBuffer<TalentTreeComponent>(e)) {
                        foundTalents = true;
                        var buffer = _EntityManager.GetBuffer<TalentTreeComponent>(e);
                        for(var i = 0; i < buffer.Length; i++) {
                            var t = buffer[i].talent;
                            var uit = Instantiate(UITalentPrefab, parent);
                            uit.UpdateTalent(t, PlayerGold.Gold);
                            uiTalents.Add(uit);
                            var index = i;
                            uit.button.onClick.AddListener(() => TryToBuyTalent(ref t, index));
                        }
                        break;
                    }
                }

                if(!foundTalents) {
                    Debug.LogWarning("Couldn't find talent entities!"); // TODO: Why can't we find the talent entities after quiting the game?
                    _needsRedraw = true;
                    return;

                }
            }
            else {
                var index = 0;
                var ents = sys.EntityManager.GetAllEntities(Allocator.TempJob);
                foreach(var e in ents) {
                    if(sys.EntityManager.HasBuffer<TalentTreeComponent>(e)) {
                        var buffer = sys.EntityManager.GetBuffer<TalentTreeComponent>(e);
                        foreach(var tc in buffer) {
                            uiTalents[index].UpdateTalent(tc.talent, PlayerGold.Gold);
                            index++;
                        }
                    }
                }
                goldValueText.SetText(PlayerGold.Gold.ToString());
                ents.Dispose();
            }
            _needsRedraw = false;
        }
    }

    public void Show() {
        transform.localPosition = Vector3.zero;
        FindObjectOfType<TitleSceneContainer>(true).Hide();
    }

    public void Hide() {
        transform.localPosition = new Vector3(-5000, -5000, 0);
        FindObjectOfType<TitleSceneContainer>(true).Show();
    }
}