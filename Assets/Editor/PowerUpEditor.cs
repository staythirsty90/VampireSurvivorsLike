using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class PowerUpEditor : EditorWindow {
    [SerializeField] public VisualTreeAsset uxml;
    VisualElement rightPane;
    ListView leftPane;
    TwoPaneSplitView splitView;
    SpriteAtlas SpriteAtlas;
    
    readonly Sprite[] allSprites                       = new Sprite[256];
    readonly List<Character>            allCharacters  = new();
    readonly List<PowerUp>              allPowerUps    = new();
    readonly List<Stat>                 allStats       = new();
    readonly List<Talent>               allTalents     = new();
    readonly List<Stage>                allStages      = new();
    readonly List<Weapon>               allWeapons     = new();
    readonly List<MissileArchetype>     allMissiles    = new();
    readonly List<PickUpArchetype>      allPickups     = new();
    readonly List<EnemyArchetype>       allEnemies     = new();

    static float panelWidth = 350;

    [MenuItem("Window/UI Toolkit/PowerUpEditor")]
    public static void ShowExample() {
        PowerUpEditor wnd = GetWindow<PowerUpEditor>();
        wnd.titleContent = new GUIContent("PowerUpEditor");

        // limit window size
        wnd.minSize = new Vector2(450, 200);
        wnd.maxSize = new Vector2(1920, 900);
    }

    class data {
        public string name;
        public EventCallback<ChangeEvent<int>> cb;
    }

    public void CreateGUI() {

        if(SpriteAtlas == null) {
            var paths = AssetDatabase.FindAssets("t:SpriteAtlas");
            SpriteAtlas = (SpriteAtlas)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(paths[0]), typeof(SpriteAtlas));
            if(SpriteAtlas == null) {
                Debug.LogError("SpriteAsset is null!");
                return;
            }
            SpriteAtlas.GetSprites(allSprites);

            foreach(var s in allSprites) {
                if(s != null) {
                    s.name = s.name.Replace("(Clone)", "");
                }
            }
        }

        var root = rootVisualElement;

        uxml.CloneTree(root);

        var datas = new data[] {    
            new () {name = "Characters",    cb = (e) => {leftPane = MakePane(typeof(Characters), allCharacters);    CleanUpSplitView();}},
            new () {name = "PowerUps",      cb = (e) => {leftPane = MakePane(typeof(PowerUps), allPowerUps);    CleanUpSplitView();}},
            new () {name = "Weapons",       cb = (e) => {leftPane = MakeWeaponPane();    CleanUpSplitView();}},
            new () {name = "Missiles",      cb = (e) => {leftPane = MakePane(typeof(Missiles), allMissiles);    CleanUpSplitView();}},
            new () {name = "Pickups",       cb = (e) => {leftPane = MakePane(typeof(PickUps), allPickups);     CleanUpSplitView();}},
            new () {name = "Enemies",       cb = (e) => {leftPane = MakePane(typeof(Enemies), allEnemies);      CleanUpSplitView();}},
            new () {name = "Stats",         cb = (e) => {leftPane = MakePane(typeof(Stats), allStats);      CleanUpSplitView();}},
            new () {name = "Talents",       cb = (e) => {leftPane = MakePane(typeof(Talents), allTalents);    CleanUpSplitView();}},
            new () {name = "Stages",        cb = (e) => {leftPane = MakePane(typeof(Stages), allStages);     CleanUpSplitView();}},
        };

        var radios = new RadioButtonGroup {
            choices = datas.Select(d => d.name).ToArray(),
        };

        radios.RegisterValueChangedCallback(
            evt => {
                datas[evt.newValue].cb.Invoke(evt);
            });

        radios.contentContainer.AddToClassList("RadioContainer");
        
        var toolbar = new VisualElement();

        toolbar.Add(radios);

        //var b = new Button { text = "Make PowerUps" };
        //b.RegisterCallback<ClickEvent>((evt) => {

        //    AssetDatabase.CreateFolder("Assets/Resources/Data/PowerUps", "Bonus");
        //    AssetDatabase.CreateFolder("Assets/Resources/Data/PowerUps", "Weapons");
        //    AssetDatabase.CreateFolder("Assets/Resources/Data/PowerUps", "Passives");

        //    foreach(var p in allPowerUps ) {
        //        var asset = ScriptableObject.CreateInstance<PowerUpSO>();
        //        asset.powerup = p;
        //        var bonuspath = $"Assets/Resources/Data/PowerUps/Bonus/{p.name}.asset";
        //        var weaponpath = $"Assets/Resources/Data/PowerUps/Weapons/{p.name}.asset";
        //        var passivepath = $"Assets/Resources/Data/PowerUps/Passives/{p.name}.asset";
        //        var path = string.Empty;
        //        if(PowerUp.IsPowerUpBonus(p)) {
        //            path = bonuspath;
        //        }
        //        else if(p._isWeapon) {
        //            path = weaponpath;
        //        }
        //        else {
        //            path = passivepath;
        //        }
                
        //        if(path == string.Empty) {
        //            Debug.LogError("path is empty!");
        //        }
        //        else {
        //            AssetDatabase.CreateAsset(asset, path);
        //        }
        //    }
        //    AssetDatabase.Refresh();
        //});
        
        //toolbar.Add(b);

        root.Add(toolbar);

        splitView = new TwoPaneSplitView(0, panelWidth, TwoPaneSplitViewOrientation.Horizontal);
        root.Add(splitView);

        //leftPane = MakePowerUpPane();
        leftPane = MakePane(typeof(PowerUps), allPowerUps);
        leftPane.selectionType = SelectionType.Multiple;
        splitView.Add(leftPane);

        rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        
        splitView.Add(rightPane);
    }

    private void CleanUpSplitView() {
        rootVisualElement.Remove(splitView);
        splitView = new TwoPaneSplitView(0, panelWidth, TwoPaneSplitViewOrientation.Horizontal);
        splitView.Add(leftPane);
        splitView.Add(rightPane);
        rootVisualElement.Add(splitView);
        leftPane.selectionType = SelectionType.Multiple;
    }

    ListView MakePane<V>(Type partialclass, in List<V> datasource) {
        var pane = new ListView();
        var methods = partialclass.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var allItems = new List<ListEntry>();
        var count = 0;
        datasource.Clear();
        var names = new List<string>();

        foreach(var item in methods) {
            if(item.ReturnType == typeof(V)) {
                var pu = (V)item.Invoke(null, null);
                datasource.Add(pu);
                names.Add(item.Name);
                count++;
            }
        }


        for(var i = 0; i < datasource.Count; i++) {
            var pu = datasource[i];
            var entry = new ListEntry();
            if(pu is PowerUp up) {
                entry.name = up.name.ToString();
                entry.spritename = up.spriteName.ToString();
                entry.isweapon = up._isWeapon;
            }
            else if(pu is Character ch) {
                entry.name = ch.charName.ToString();
                entry.spritename = ch.idleSpriteNames[0].ToString();
            }
            else if(pu is Weapon w) {
                entry.name = "Weapon";
                entry.spritename = null;
            }
            else if(pu is MissileArchetype m) {
                entry.name = names[i];
                entry.spritename = m.gfx.spriteName.ToString();
            }
            else if(pu is PickUpArchetype p) {
                entry.name = names[i];
                entry.spritename = p.gfx.spriteName.ToString();
            }
            else if(pu is EnemyArchetype ea) {
                entry.name = names[i];
                entry.spritename = ea.gfx.spriteName.ToString();
            }
            else if(pu is Stat s) {
                entry.name = names[i];
                entry.spritename = s.iconName.ToString();
            }
            else if(pu is Talent t) {
                entry.name = names[i];
                entry.spritename = t.Icon.ToString();
            }
            else if(pu is Stage stage) {
                entry.name = stage.stageName;
                entry.spritename = null;
            }
            
            allItems.Add(entry);
        }


        allItems = allItems
            .OrderBy(item => item.isweapon)
            .ThenBy(item => item.name)
            .Prepend(new ListEntry { name = $"Total {partialclass.Name}: {count}" })
            .ToList();
        
        // init the list view
        pane.makeItem = () => {
            var entry = new PowerUpVisualElement();
            return entry;
        };
        pane.itemsSource = allItems;

        pane.bindItem = (item, index) => {
            var puv = (item as PowerUpVisualElement);
            var name = allItems[index].name;
            puv.label.text = name;
            var sname = PatchName(allItems[index].spritename);
            var sprite = allSprites.FirstOrDefault(s => s.name == sname);
            PatchSprite(ref sprite);
            puv.image.sprite = sprite;

            puv.AddToClassList("ListEntry");
        };

        pane.fixedItemHeight = 64;

        // selection reaction
        pane.selectionChanged += OnSpriteSelectionChange;

        return pane;
    }
    
    ListView MakeWeaponPane() {
        var pane = new ListView();
        
        var allItems = new List<ListEntry>();

        var count = 0;

        allWeapons.Clear();

        allItems.Add(new ListEntry());

        foreach(var pu in allPowerUps) {
            var i = 0;
            var prevname = pu.name;
            foreach(var w in pu.Weapons) {
                var entry = new ListEntry();
                if(prevname == pu.name) {
                    entry.name = $"Weapon ({pu.name}) - [{i}]";
                }
                else {
                    entry.name = $"Weapon ({pu.name})";
                }
                entry.spritename = pu.spriteName.ToString();
                allItems.Add(entry);
                count++;
                i++;
            }
        }

        allItems[0].name = $"Total Weapons: {count}";

        // init the list view
        pane.makeItem = () => new PowerUpVisualElement();
        pane.itemsSource = allItems;

        pane.bindItem = (item, index) => {
            var puv = (item as PowerUpVisualElement);
            var name = allItems[index].name;
            puv.label.text = name;
            var sname = PatchName(allItems[index].spritename);
            var sprite = allSprites.FirstOrDefault(s => s.name == sname);
            PatchSprite(ref sprite);
            puv.image.sprite = sprite;
            puv.AddToClassList("ListEntry");
        };

        pane.fixedItemHeight = 64;

        // selection reaction
        //pane.selectionChanged += OnSpriteSelectionChange;

        return pane;
    }

    public class ListEntry {
        public string spritename;
        public string name;
        public bool isweapon;
    }
    
    private void PatchSprite(ref Sprite s) {
        if(s == null) {
            s = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.animation/Editor/Assets/EditorIcons/Dark/d_ErrorIcon_Small@2x.png");
        }
    }

    private static string PatchName(string name) {
        if(string.IsNullOrEmpty(name) || name == "none") {
            name = "question_mark";
        }

        return name;
    }

    private void OnSpriteSelectionChange(IEnumerable<object> selectedItems) {
        rightPane.Clear();
        var enumerator = selectedItems.GetEnumerator();
        if(enumerator.MoveNext()) {
            if(enumerator.Current is ListEntry selected) {
                {
                    var pu = allPowerUps.Find(p => p.name == selected.name);
                    if(!pu.name.IsEmpty) {
                        var scrollView = new ScrollView();
                        var t = Resources.LoadAll<PowerUpSO>("Data/PowerUps/");
                        t[0].powerup = pu;
                        scrollView.Add(new InspectorElement(t[0]));
                        rightPane.Add(scrollView);
                        return;
                    }
                }
                var ch = allCharacters.Find(p => p.charName == selected.name);
                if(!ch.charName.IsEmpty) {
                    var scrollView = new ScrollView();
                    //var t = Resources.LoadAll<PowerUpSO>("Data/PowerUps/");
                    //t[0].powerUp = pu;
                    //scrollView.Add(new InspectorElement(t[0]));
                    rightPane.Add(scrollView);
                }
            }
        }
    }

    public class PowerUpVisualElement : VisualElement {
        public Image image = new() {
            scaleMode = ScaleMode.ScaleToFit,
        };
        public Label label = new();

        public PowerUpVisualElement() {
            Add(image);
            Add(label);
        }
    }
}
