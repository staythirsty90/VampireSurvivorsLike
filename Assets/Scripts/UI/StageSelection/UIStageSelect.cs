using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStageSelect : MonoBehaviour {
    public GameObject UIStagePrefab;
    List<Stage> stages = new();
    Scrollbar Scrollbar;
    Transform ContentTransform;
    sbyte selectedStageIndex = -1;
    UICharacterSelect UICharacterSelect;
    Button continueButton;

    private void Awake() {
        Scrollbar = GetComponentInChildren<Scrollbar>();
        Debug.Assert(Scrollbar != null);

        ContentTransform = GetComponentInChildren<ContentSizeFitter>().transform;
        Debug.Assert(ContentTransform != null);

        UICharacterSelect = FindObjectOfType<UICharacterSelect>(true);
        Debug.Assert(UICharacterSelect != null);

        continueButton = transform.GetChild(0).Find("Continue Button").GetComponent<Button>();
        Debug.Assert(continueButton != null);
        continueButton.onClick.AddListener(() => { OnContinueClicked(); });
        continueButton.interactable = false;

        foreach(var b in transform.GetComponentsInChildren<Button>()) {
            if(b.name == "Back Button") {
                b.onClick.AddListener(() => { Back(); });
                break;
            }
        }
    }

    void Start() {
        ReInitStages();

        sbyte i = 0;
        foreach(var s in stages) {
            //Debug.Log(s.stageName);

            var go = Instantiate(UIStagePrefab, ContentTransform);

            var title = go.transform.Find("Name").GetComponent<TextMeshProUGUI>();
            var desc = title.transform.Find("Desc").GetComponent<TextMeshProUGUI>();
            var icon = go.transform.Find("Icon").GetComponentInChildren<Image>();

            title.SetText(s.stageName);
            desc.SetText(s.description);
            icon.sprite = SpriteDB.Instance.Get(s.iconName);

            var button = go.GetComponent<Button>();
            Debug.Assert(button != null);
            
            var index = i;
            button.onClick.AddListener(() => { ChooseStage(index); });
            i++;
        }
        
        // Snap the scroll view to the top because Unity can't do it on its own....
        Canvas.ForceUpdateCanvases();
        Scrollbar.value = 1;

        Hide();
    }

    public void ReInitStages() {
        stages.Clear();
        var methods = typeof(Stages).GetMethods();
        foreach(var m in methods) {
            if(!m.ReturnType.Equals(typeof(Stage)))
                continue;

            var stage = (Stage)m.Invoke(null, null);
            if(stage == null) continue;
            stages.Add(stage);
        }
    }

    public Stage GetStage() {
        if(selectedStageIndex == -1)
            return stages[0];
        else
            return stages[selectedStageIndex];
    }

    public void Show() {
        gameObject.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public void Back() {
        Debug.Log($"going back selectedStageIndex: {selectedStageIndex} ");
        selectedStageIndex = -1;
        continueButton.interactable = false;
        Hide();
    }

    public void ChooseStage(sbyte index) {
        Debug.Log($"Choosing stage index {index}");
        selectedStageIndex = index;
        continueButton.interactable = true;
    }

    public void OnContinueClicked() {
        UICharacterSelect.Show();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape) && !UICharacterSelect.IsShowing()) {
            Back();
        }
    }
}