using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FramerateSlider : MonoBehaviour {
    [SerializeField]
    string Parameter;
    Slider Slider;

    private void Start() {
        Slider = GetComponent<Slider>();
        Debug.Assert(Slider);
        Debug.Assert(!string.IsNullOrEmpty(Parameter));
        Slider.value = PlayerPrefs.GetFloat(Parameter, 60);
        //Debug.Log($"Loading {Parameter}: {Slider.value}");
        Slider.onValueChanged.AddListener(SetLevel);
        SetLevel(Slider.value);
    }

    public void SetLevel(float value) {
        var converted = (int)value;
        Application.targetFrameRate = converted;
        PlayerPrefs.SetFloat(Parameter, converted);
        //Debug.Log($"Setting {Parameter} to {converted}");
    }
}