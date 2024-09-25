using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour {

    public static event EventHandler OnOptionsClosed;

    private void Awake() {
        foreach(var b in transform.GetComponentsInChildren<Button>()) {
            if(b.name == "Close Button") {
                b.onClick.AddListener(() => { Hide(); });
                break;
            }
        }
    }

    private void Start() {
        Hide();
    }

    public void Hide() {
        gameObject.SetActive(false);
        OnOptionsClosed?.Invoke(this, null);
    }

    public void Show() {
        gameObject.SetActive(true);
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            Hide();
        }
    }
}