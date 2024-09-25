using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using System.Linq;
using Unity.Entities;

public class UICharacterSelect : MonoBehaviour {
    public float ovalWidth = 600;
    public float ovalHeight = 300;
    public float2 offset = new (0, 100f);

    readonly List<Character> chars = new();
    Transform Carousel;
    sbyte selectedCharacterIndex = 0;
    PlayGameButton PlayGameButton;
    CharacterSelect CharacterSelect;
    TextMeshProUGUI title;
    TextMeshProUGUI desc;
    Image startingWeaponIcon;
    readonly List<UIStat> UIStats = new();
    readonly List<float3> wallPositions = new();
    RectTransform[] rectTransforms;
    public float currentAngle = 0;
    float angleDifference;
    sbyte direction = 1;
    bool lerping;
    float[] angles;

    private void Awake() {
        CharacterSelect = FindObjectOfType<CharacterSelect>(true);
        Debug.Assert(CharacterSelect != null);

        Carousel = GameObject.Find("Characters Carousel").transform;
        Debug.Assert(Carousel != null);

        PlayGameButton = FindObjectOfType<PlayGameButton>(true);
        Debug.Assert(PlayGameButton != null);
        PlayGameButton.GetComponent<Button>().interactable = false;

        foreach(var b in transform.GetComponentsInChildren<Button>()) {
            if(b.name == "Back Button") {
                b.onClick.AddListener(() => { Back(); });
                break;
            }
        }

        { // Find and invoke all Character methods;
            var methods = typeof(Characters).GetMethods();
            var i = 0;
            foreach(var m in methods) {
                if(!m.ReturnType.Equals(typeof(Character)))
                    continue;

                if(m.Name.Contains("Base")) continue; // skip the make base character method

                var chara = (Character)m.Invoke(null, null);
                if(chara.Equals(default(Character))) continue;
                chars.Add(chara);
                i++;
            }
        }

        var statsParent = GameObject.Find("Character Stats").transform;
        var uistatPrefab = statsParent.GetComponentInChildren<UIStat>(true);

        var character = chars[selectedCharacterIndex];
        foreach(var kvp in character.CharacterStats.stats) {
            if(kvp.Value.invisible) {
                continue;
            }
            var uistat = Instantiate(uistatPrefab, statsParent);
            uistat.gameObject.SetActive(true);
            uistat.gameObject.name = $"Stat:{kvp.Value.name}";
            UIStats.Add(uistat);
        }

        var go = GameObject.Find("Character Info");
        Debug.Assert(go != null);

        title = go.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        desc = go.transform.Find("Description").GetChild(0).GetComponent<TextMeshProUGUI>();
        startingWeaponIcon = go.transform.Find("Icon Stat").GetComponent<Image>();

        var amount = chars.Count;
        var angle = 360f / amount;
        angles = new float[amount];
        for(var i = 0; i < amount; i++) {
            angles[i] = angle * i;
            //Debug.Log(angles[i]);
        }

        InitPositions(amount);

        var chars_reversed = chars.Reverse<Character>().ToArray();
        for(var j = 0; j < chars_reversed.Length; j++) {
            var ch = chars_reversed[j];
            var characterGraphic                            = new GameObject().AddComponent<Image>();
            characterGraphic.sprite                         = SpriteDB.Instance.Get(ch.idleSpriteNames[0]);
            characterGraphic.transform.SetParent(Carousel);
            characterGraphic.rectTransform.anchorMin        = new Vector2(0.5f, 0);
            characterGraphic.rectTransform.anchorMax        = new Vector2(0.5f, 0);
            characterGraphic.rectTransform.pivot            = new Vector2(0.5f, 0);
            characterGraphic.rectTransform.anchoredPosition = new Vector2(wallPositions[j].x, wallPositions[j].y);
            characterGraphic.SetNativeSize();
            characterGraphic.name                           = ch.charName.ToString();
        }
        rectTransforms = Carousel.GetComponentsInChildren<RectTransform>().Skip(1).ToArray();

        MoveCharactersIntoPosition(amount);

        Hide();
    }
    
    private void OnEnable() {
        var character = chars[selectedCharacterIndex];
        SetCharacterInfo(character);
        SetUIStats(character);
        ChooseCharacter(selectedCharacterIndex);
        //Debug.Log($"Selected Char Index: {selectedCharacterIndex}");
    }

    readonly List<Stat> _sortedStats = new ();
    void SetUIStats(Character ch) {
        _sortedStats.Clear();
        var i = 0;
        foreach(var kvp in ch.CharacterStats.stats) {
            if(kvp.Value.invisible) {
                continue;
            }
            _sortedStats.Add(kvp.Value);
            i++;
        }
        
        _sortedStats.Sort((a, b) => a.name.ToString().ToLower().CompareTo(b.name.ToString().ToLower()));
        i = 0;
        foreach(var stat in _sortedStats) {
            UIStats[i].SetStat(stat);
            i++;
        }
    }

    private void Update() {

        if(Input.GetKeyDown(KeyCode.Escape)) {
            Back();
            return;
        }

        var amount = rectTransforms.Length;
        var angle = 360f / amount;

        if(lerping) {
            var desiredAngle = angles[selectedCharacterIndex];
            var timeToRotate = 0.3f;
            var speed = angle / timeToRotate;
            angleDifference -= Time.deltaTime * speed;
            currentAngle += Time.deltaTime * speed * direction;
            if(angleDifference <= 0) {
                lerping = false;
                currentAngle = desiredAngle;
                angleDifference = 0;
                SetCharacterInfo(chars[selectedCharacterIndex]);
                SetUIStats(chars[selectedCharacterIndex]);
                ChooseCharacter(selectedCharacterIndex);
            }
        }
        else {
            if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
                direction = RotateChars(1);
            }
            else if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
                direction = RotateChars(-1);
            }
        }
        InitPositions(amount);
        MoveCharactersIntoPosition(amount);
    }

    private void MoveCharactersIntoPosition(int amount) {
        for(var j = 0; j < amount; j++) {
            var pos = wallPositions[j];
            var yscale = (pos.y - offset.y) / ovalHeight;

            yscale = math.clamp(yscale, 0.7f, 1);
            var xscale = yscale;
            var rt = rectTransforms[j];
            rt.anchoredPosition = new Vector2(pos.x, pos.y);
            rt.localScale = new Vector3(xscale, yscale, 1);
        }
    }

    private float2 InitPositions(int amount) {
        var angle = 360f / amount;
        wallPositions.Clear();
        var angleOffset = 1f;
        for(var i = 0; i < amount; i++) {
            var x = math.sin(math.radians(currentAngle + angleOffset * ((i + 1) * angle))) * ovalWidth + offset.x;
            var y = math.cos(math.radians(currentAngle + angleOffset * ((i + 1) * angle))) * ovalHeight + offset.y;
            var spawnPosition = new float3(x, y, 0);
            wallPositions.Add(spawnPosition);
        }

        return offset;
    }

    private sbyte RotateChars(sbyte direction) {
        selectedCharacterIndex += direction;
        if(selectedCharacterIndex < 0) {
            selectedCharacterIndex = (sbyte)(chars.Count - 1);
        }
        else if(selectedCharacterIndex >= chars.Count) {
            selectedCharacterIndex = 0;
        }
        lerping = true;
        angleDifference = 360f / chars.Count;
        return direction;
    }

    void SetCharacterInfo(Character ch) {
        var lootsystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        var pu_name = ch.startingWeapons[0];
        var pu = lootsystem.GetPowerUp(pu_name);
        startingWeaponIcon.sprite = SpriteDB.Instance.Get(ch.startingWeapons.IsEmpty || ch.startingWeapons.Length == 0 ? "question_mark" : pu.spriteName); // TODO: Show multiple starting items, if any.
        title.SetText(ch.charName.ToString());
        desc.SetText(ch.desc.ToString());
    }

    public void Show() {
        gameObject.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public bool IsShowing() {
        return gameObject.activeInHierarchy;
    }

    public void Back() {
        PlayGameButton.GetComponent<Button>().interactable = false;
        Hide();
    }

    public Character GetDefaultCharacter() {
        //Debug.Log($"Getting default char index {0}");
        selectedCharacterIndex = 0;
        return chars[0];
    }

    public void ChooseCharacter(sbyte index) {
        //Debug.Log($"Choosing char index {index}");
        selectedCharacterIndex = index;
        PlayGameButton.GetComponent<Button>().interactable = true;
        CharacterSelect.SetCharacter(chars[selectedCharacterIndex]);
    }
}