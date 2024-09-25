using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionsButton : MonoBehaviour {
    public static event EventHandler OnOptionsPressed;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { OptionsPressed(); });
    }

    void OptionsPressed() {
        OnOptionsPressed?.Invoke(this, null);
    }
}