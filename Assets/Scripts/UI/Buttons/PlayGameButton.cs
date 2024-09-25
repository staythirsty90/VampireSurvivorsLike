using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayGameButton : MonoBehaviour {

    public static event EventHandler OnPlayGame;
    bool clicked;
    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { PlayGame(); });
    }

    public void PlayGame() {
        if(!clicked) {
            clicked = true;
            OnPlayGame?.Invoke(this, null);
        }
    }
}