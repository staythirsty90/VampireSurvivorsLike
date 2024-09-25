using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class FullScreenModeDropdown : MonoBehaviour {
    void Start() {
        var Dropdown = GetComponent<TMP_Dropdown>();
        Debug.Assert(Dropdown);
        Dropdown.ClearOptions();
        var index = -1;
        var options = new List<string>();
        var modes = Enum.GetNames(typeof(FullScreenMode));
        for(var i = 0; i < modes.Length; i++) {
            var mode = modes[i];
            if((FullScreenMode)i == Screen.fullScreenMode) {
                index = i;
            }
            options.Add(mode.ToString());
        }
        Dropdown.AddOptions(options);
        Dropdown.value = index;
        Dropdown.RefreshShownValue();

        Dropdown.onValueChanged.AddListener((index) => { SetFullScreenMode(index); });
    }

    public void SetFullScreenMode(int index) {
        Screen.fullScreenMode = (FullScreenMode)index;
    }
}