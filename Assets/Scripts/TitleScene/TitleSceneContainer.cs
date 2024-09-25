using UnityEngine;

public class TitleSceneContainer : MonoBehaviour {

    public void Show() {
        transform.GetChild(0).gameObject.SetActive(true);
    }

    public void Hide() {
        transform.GetChild(0).gameObject.SetActive(false);
    }
}