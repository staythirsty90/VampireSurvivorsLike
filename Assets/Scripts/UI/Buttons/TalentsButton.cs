using System;
using UnityEngine;
using UnityEngine.UI;

public class TalentsButton : MonoBehaviour {
    public static event EventHandler OnTalentsPressed;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { ShowTalents(); });
    }

    public void ShowTalents() {
        OnTalentsPressed?.Invoke(this, null);
    }
}