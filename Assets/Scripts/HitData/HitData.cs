using Unity.Entities;
using UnityEngine;

public struct HitData : IComponentData {
    public Color hitColor;
    public Color baseColor;
    public float startTime;
    public float duration;

    public static void Reset(HitData hitData) {
        hitData.startTime = 0;
    }
}