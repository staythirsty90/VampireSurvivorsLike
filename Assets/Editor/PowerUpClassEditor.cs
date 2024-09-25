using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

[CustomEditor(typeof(PowerUpSO))]
public class PowerUpClassEditor : Editor {

    //public override VisualElement CreatePropertyGUI(SerializedProperty property) {
    //    //return new PropertyField(property);

    //    var root = new VisualElement();
    //    PowerUp p = (PowerUp)property.boxedValue;

    //    root.Add(new TextField { label = "Name", value = p.name.ToString() });
    //    root.Add(new TextField { label = "Description", value = p.description.ToString() });
    //    root.Add(new TextField { label = "Evolution", value = p.evolutionName.ToString() });
    //    root.Add(new TextField { label = "Evolution Requirement", value = p.requiresName.ToString() });

    //    var put = new EnumField { label = "PowerUp Type" };
    //    put.Init(p.PowerUpType);
    //    root.Add(put);

    //    put = new EnumField { label = "PowerUp Effect" };
    //    put.Init(p.PowerUpEffect);
    //    root.Add(put);

    //    root.Add(new Toggle { name = "IsWeapon", label = "Is Weapon?", value = p._isWeapon });
    //    root.Add(new Toggle {                    label = "Is Evolution?", value = p.isEvolution });

    //    root.Add(new TextField { label = "Max Level", value = p.maxLevel.ToString() });
    //    root.Add(new TextField { label = "Rarity", value = p.rarity.ToString() });
    //    root.Add(new ObjectField { label = "Particle System", objectType = typeof(ParticleSystem), value = null, allowSceneObjects = false });

    //    root.Q<Toggle>("IsWeapon").RegisterValueChangedCallback((evt) => {
    //        Debug.Log(p.name);
    //        var wpath = $"Assets/Resources/Data/PowerUps/Weapons/{p.name}.asset";
    //        var ppath = $"Assets/Resources/Data/PowerUps/Passives/{p.name}.asset";

    //        if(evt.newValue == true) {
    //            Debug.Log($"{AssetDatabase.MoveAsset(ppath, wpath)}, w: {wpath}, p: {ppath}");
    //        }
    //        else {
    //            Debug.Log($"{AssetDatabase.MoveAsset(wpath, ppath)}, w: { wpath}, p: { ppath}");
    //        }
    //        AssetDatabase.Refresh();
    //    });

    //    //var b = new Button { text = "Try Make Asset" };
    //    //b.RegisterCallback<ClickEvent>((evt) => { 
    //    //    Debug.Log("Clicked");
    //    //    var asset = ScriptableObject.CreateInstance<PowerUpSO>();
    //    //    asset.powerUp = p;
    //    //    AssetDatabase.CreateAsset(asset, $"Assets/Resources/Data/PowerUps/{p.name}.asset");
    //    //    AssetDatabase.Refresh();
    //    //});

    //    //root.Add(b);

    //    return root;
    //    //return new PropertyField(property);
    //}

    public override VisualElement CreateInspectorGUI() {

        var root = new VisualElement();
        //InspectorElement.FillDefaultInspector(root, serializedObject, this);
        ////Debug.Log("showing custom inspector");

        var p = (PowerUp)serializedObject.FindProperty("powerup").boxedValue;

        root.Add(new TextField { label = "Name", value = p.name.ToString() });
        root.Add(new TextField { label = "Sprite Name", value = p.spriteName.ToString() });
        var opensprite = new Button { text = "Edit Sprite" };

        var path = "Assets/Sprites/";
        opensprite.RegisterCallback<ClickEvent>((evt) => {
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { path });

            foreach(var item in sprites) {
                var s = AssetDatabase.GUIDToAssetPath(item);
                if(s.Contains(p.spriteName.ToString())) {
                    var a = s.Split(path)[1];
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal($"{path}/{a}", 0);
                }
            }
        });
        root.Add(opensprite);
        root.Add(new TextField { label = "Description", value = p.description.ToString()});
        root.Add(new ObjectField { label = "Evolution", objectType = typeof(PowerUpSO), allowSceneObjects = false});
        root.Add(new ObjectField { label = "Evolution Requirement", objectType = typeof(PowerUpSO), allowSceneObjects = false });

        if(p._isWeapon) {
            var put = new EnumField { label = "PowerUp Type" };
            put.Init(p.PowerUpType);
            root.Add(put);
        }

        {
            var put = new EnumField { label = "PowerUp Effect" };
            put.Init(p.PowerUpEffect);
            root.Add(put);
        }

        root.Add(new Toggle { name = "IsWeapon", label = "Is Weapon?", value = p._isWeapon });
        root.Add(new Toggle { label = "Is Evolution?", value = p.isEvolution });

        root.Add(new TextField { label = "Max Level", value = p.maxLevel.ToString() });
        root.Add(new TextField { label = "Rarity", value = p.rarity.ToString() });
        root.Add(new ObjectField { label = "Particle System", objectType = typeof(ParticleSystem), value = null, allowSceneObjects = false });

        //Stats.InitTables();

        //root.Add(new Label { text = "Affected Stats" });
        //var size = serializedObject.FindProperty("affectedStats").arraySize;
        //for(var i = 0; i < size; i++) {
        //    var item = (StatIncrease)serializedObject.FindProperty("affectedStats").GetArrayElementAtIndex(i).boxedValue;
        //    root.Add(new Label { text = Stats.StatTable[item.statType].Name });
        //    root.Add(new Toggle { label = "Is Percentage Based?", value = item.isPercentageBased });
        //    root.Add(new FloatField { label = "Value", value = item.value});
        //}

        //root.Q<Toggle>("IsWeapon").RegisterValueChangedCallback((evt) => {
        //    var name = serializedObject.targetObject.name;
        //    var wpath = $"Assets/Resources/Data/PowerUps/Weapons/{name}.asset";
        //    var ppath = $"Assets/Resources/Data/PowerUps/Passives/{name}.asset";

        //    serializedObject.FindProperty("powerUp").FindPropertyRelative("_isWeapon").boolValue = evt.newValue;
        //    if(serializedObject.ApplyModifiedProperties()) {
        //        Debug.Log($"setting bool to {evt.newValue}, old: {evt.previousValue}, new: {evt.newValue}");
        //    }
        //    else {
        //        Debug.Log("couldnt apply modified properties...");
        //    }

        //    if(evt.newValue == true && evt.previousValue == false) {
        //        Debug.Log($"{AssetDatabase.MoveAsset(ppath, wpath)}, w: {wpath}, p: {ppath}");
        //    }
        //    else if(evt.newValue == false && evt.previousValue == true) {
        //        Debug.Log($"{AssetDatabase.MoveAsset(wpath, ppath)}, w: { wpath}, p: { ppath}");
        //    }
        //});

        var openbutton = new Button {
            text = "Open Script"
        };

        openbutton.RegisterCallback<ClickEvent>((evt) => {
        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal($"Assets/Scripts/PowerUps/PowerUp.{p.name}.cs", 0);
        });

        root.Add(openbutton);

        return root;
    }
}