using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioSlider : MonoBehaviour {
    [SerializeField]
    string Parameter;
    [SerializeField]
    AudioMixer AudioMixer;
    Slider Slider;

    private void Start() {
        Debug.Assert(AudioMixer);
        Slider = GetComponent<Slider>();
        Debug.Assert(Slider);
        Debug.Assert(!string.IsNullOrEmpty(Parameter));
        Slider.value = PlayerPrefs.GetFloat(Parameter, 0.75f);
        //Debug.Log($"Loading {Parameter}: {Slider.value}");
        Slider.onValueChanged.AddListener(SetLevel);
        SetLevel(Slider.value);
    }

    public void SetLevel(float value) {
        var converted = Mathf.Log10(value) * 20;
        AudioMixer.SetFloat(Parameter, converted);
        PlayerPrefs.SetFloat(Parameter, value);
        //Debug.Log($"Setting audio mixer {Parameter} to {converted}");
    }
}