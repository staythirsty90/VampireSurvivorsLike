using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ExitGameButton : MonoBehaviour {
    bool clicked;

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { OnExitClicked(); });
    }

    public void OnExitClicked() {
        if(!clicked) {
            clicked = true; // why bother preventing multiple clicks if we're exiting?
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}