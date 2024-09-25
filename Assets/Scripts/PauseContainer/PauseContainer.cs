using UnityEngine;

public class PauseContainer : MonoBehaviour {
    PauseGameButton PauseGameButton;
    
    private void Start() {
        PauseGameButton = FindObjectOfType<PauseGameButton>(true);
        Debug.Assert(PauseGameButton != null);
        Hide();
    }

    public void Show() {
        gameObject.SetActive(true);
        PauseGameButton.SetToUnpause();
    }

    public void Hide() {
        PauseGameButton.SetToPause();
        gameObject.SetActive(false);
    }
}