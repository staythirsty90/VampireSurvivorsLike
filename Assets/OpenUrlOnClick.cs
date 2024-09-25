using UnityEngine;

public class OpenUrlOnClick : MonoBehaviour {

    public void OnClick(string url) {
        if(string.IsNullOrEmpty(url)) {
            return;
        }
        Application.OpenURL(url);
    }
}