using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitGameButton : MonoBehaviour {

    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => { OnQuitClicked(); });
    }

    bool clicked;
    public void OnQuitClicked() {
        if(!clicked) {
            clicked = true;
            
            World.DefaultGameObjectInjectionWorld.Dispose();
            SceneManager.LoadScene("Main");
        }
    }
}