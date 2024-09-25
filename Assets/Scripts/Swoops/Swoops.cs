using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Swoops : IComponentData {
    public bool swooping;
    public bool grabbed;
    public float swoopStartTime;
    public float distance;
    public float3 swoopStartPosition;
    public float3 swoopDirection;
}