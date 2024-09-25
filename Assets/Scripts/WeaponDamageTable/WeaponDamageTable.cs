using Unity.Collections;
using UnityEngine;

public class WeaponDamageTable : MonoBehaviour {
    public static NativeArray<float> Table;

    void Awake() {
        if(Table.IsCreated) {
            Table.Dispose();
        }
        Table = new NativeArray<float>(128, Allocator.Persistent);
    }

    private void OnDestroy() {
        Table.Dispose();
    }
}