using UnityEngine;
using UnityEngine.UI;

public class PauseGameButton : MonoBehaviour {

    [SerializeField]
    Sprite pauseSprite;
    [SerializeField]
    Sprite unpauseSprite;

    Image Image;

    private void Awake() {
        GetImage();
    }

    public void SetToPause() {
        if(Image == null) {
            GetImage();
        }
        Image.sprite = pauseSprite;
    }

    private void GetImage() {
        Image = GetComponent<Image>();
    }

    public void SetToUnpause() {
        Image.sprite = unpauseSprite;
    }
}