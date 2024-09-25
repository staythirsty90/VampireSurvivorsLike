using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(TMP_Dropdown))]
public class ResolutionDropdown : MonoBehaviour {
    
    readonly List<Resolution> supportedResolutions = new();
    const float ratio = 16 / 9f;

    void Start() {
        var Dropdown = GetComponent<TMP_Dropdown>();
        Debug.Assert(Dropdown);
        for(var i = 0; i < Screen.resolutions.Length; i++) {
            var res = Screen.resolutions[i];
            var ar = res.width / (float)res.height;
            //Debug.Log($"{res.width}x{res.height}, ar: {ar}");

            // TODO: Properly handle any edge cases where the resolution may be changed to an undesired aspect ratio.
            if(!Mathf.Approximately(ar, ratio)) {
                continue;
            }

            supportedResolutions.Add(res);
        }

        Dropdown.ClearOptions();
        var index = -1;
        var options = new List<string>();
        
        for(var i = 0; i < supportedResolutions.Count; i++) {
            var res = supportedResolutions[i];
            if(res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height) {
                index = i;
            }
            options.Add(res.ToString()/*.Split("@")[0]*/);
        }

        Debug.Assert(index != -1, "dropdown index was -1!");
        Dropdown.AddOptions(options);
        Dropdown.value = index;
        Dropdown.RefreshShownValue();
        Dropdown.onValueChanged.AddListener((index) => { SetResolution(index); });
    }

    IEnumerator WaitToScroll() {
        var Dropdown = GetComponent<TMP_Dropdown>();
        while(!Dropdown.IsExpanded) {
            yield return null;
        }
        var scrollbar = transform.Find("Dropdown List").GetComponentInChildren<Scrollbar>();
        if(scrollbar != null ) {
            scrollbar.value = PlayerPrefs.GetFloat("ResolutionDropdownScrollValue", 1f);
        }
    }

    public void Test() {
        StartCoroutine(WaitToScroll());
        Debug.Log("Clicked Dropdown!");
    }

    public void SetResolution(int index) {
        var scrolledAmount = transform.Find("Dropdown List").GetComponentInChildren<Scrollbar>().value;
        PlayerPrefs.SetFloat("ResolutionDropdownScrollValue", scrolledAmount);
        Debug.Log(scrolledAmount);
        var w = supportedResolutions[index].width;
        var h = supportedResolutions[index].height;
        var rf = supportedResolutions[index].refreshRateRatio;
        Screen.SetResolution(w, h, Screen.fullScreenMode, rf);
        Debug.LogError($"Setting resolution to {w}x{h}, index: {index}");
    }
}