using System;
using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour {

    public static event EventHandler OnStartGame;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { StartGame(); });
    }

    void StartGame() {
        OnStartGame?.Invoke(this, null);
    }
}