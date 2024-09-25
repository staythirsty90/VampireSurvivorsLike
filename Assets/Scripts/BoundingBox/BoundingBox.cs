using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

public struct BoundingBox : IComponentData {
    public Vector3 center;
    public Vector3 size;
    public Vector3 extents;
    public Vector3 max;
    public Vector3 min;
    public float rotationAngle;
    public bool flip;
    public Vector3 pivot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBounds(Vector3 center, Vector3 size, bool flip = false, float rotationAngle = 0) {
        extents = size * 0.5f;
        this.size = size;
        this.rotationAngle = rotationAngle;
        this.center = center;
        max = this.center + extents;
        min = this.center - extents;

        this.flip = flip;
        pivot = this.center;
    }
}