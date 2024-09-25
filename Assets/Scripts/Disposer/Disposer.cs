using UnityEngine;

public class Disposer : MonoBehaviour {

    private void OnApplicationQuit() {
        Missiles.Dispose();
        Enemies.Dispose();
        PickUps.Dispose();
    }
}