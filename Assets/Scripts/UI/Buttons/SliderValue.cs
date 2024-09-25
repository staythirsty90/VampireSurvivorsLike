using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SliderValue : MonoBehaviour {

    public enum SliderValueDisplayType {
        AsIs,
        Mult100,
    }
    [SerializeField]
    SliderValueDisplayType displayType;
    [SerializeField]
    string append;
    [SerializeField]
    Slider slider;
    TextMeshProUGUI textMeshPro;


    private void Start() {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        Debug.Assert(textMeshPro != null);
        Debug.Assert(slider != null, $"The SliderValue ({gameObject.name}) doesn't have a Slider component to reference!");
        slider.onValueChanged.AddListener(SetValue); // TODO: Does this leak if we dont remove it?
        SetValue(slider.value);
    }

    private void SetValue(float value) {

        switch(displayType) {
            case SliderValueDisplayType.AsIs:
                textMeshPro.SetText($"{value}{append}");
                break;
            
            case SliderValueDisplayType.Mult100:
                textMeshPro.SetText($"{(int)(value * 100)}{append}");
                break;
        }
    }
}