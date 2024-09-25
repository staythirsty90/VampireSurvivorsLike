using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct DamageNumberIndex : IComponentData {
    public byte currentDamgeNumberIndex;

    public static byte GetNext(ref DamageNumberIndex index, in DynamicBuffer<DamageNumberData> buffer) {
        var idx = index.currentDamgeNumberIndex;
        index.currentDamgeNumberIndex++;
        if(index.currentDamgeNumberIndex >= buffer.Capacity) {
            index.currentDamgeNumberIndex = 0;
        }
        return idx;
    }
}

[InternalBufferCapacity(32)]
public struct DamageNumberData : IBufferElementData {
    public bool shouldDraw;
    public float timeAlive;
    public byte value1;
    public byte value2;
    public byte value3;
    public byte value4;
    public float3 position1;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawDamageNumberMeshSystem : SystemBase {
    Material material;
    readonly MaterialPropertyBlock mpb = new();
    NativeArray<Vector2> directions;
    readonly int digit_prop = Shader.PropertyToID("_Digits");
    readonly int color_prop = Shader.PropertyToID("_Alphas");
    const int cap = 1023;

    Vector4[] digits;
    Matrix4x4[] matrix;
    float[] alphas;
    float[] timers;

    Mesh pointMesh;

    protected override void OnCreate() {
        base.OnCreate();
        material = Resources.Load<Material>("Materials/DamageNumbers");

        directions = new NativeArray<Vector2>(3, Allocator.Persistent);
        GenerateArcDirections(ref directions);

        digits = new Vector4[cap];
        matrix = new Matrix4x4[cap];
        alphas = new float[cap];
        timers = new float[cap];

        pointMesh = new Mesh {
            vertices = new Vector3[] {
                new (0,0,0),
            }
        };
        pointMesh.SetIndices(new int[] { 0 }, MeshTopology.Points, 0);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        directions.Dispose();
    }

    protected override void OnUpdate() {

        var _a = new NativeList<float>(cap * 4, Allocator.TempJob);
        var _t = new NativeList<float>(cap * 4, Allocator.TempJob);
        var _d = new NativeList<Vector4>(cap * 4, Allocator.TempJob);
        var _m = new NativeList<Matrix4x4>(cap * 4, Allocator.TempJob);

        var _dirs = directions;
        var shouldDrawGlobal = false;

        Entities
            .WithName("DrawMesh")
            .ForEach((in DynamicBuffer<DamageNumberData> buffer/*, in Translation pos*/) => {
                var offset = new float3(0.2f, 0f, 0f);
                var length = buffer.Length;
                for(int i = 0; i < length; i++) {
                    var dnData = buffer[i];
                    if(!dnData.shouldDraw) continue;

                    var alive = dnData.timeAlive;
                    var alpha = alive > 0.75f ? math.lerp(4f, 0, alive) : 1;

                    var scale = new Vector3(0.25f, 0.25f, 0.25f);
                    var s = 0f;

                    s = Remap(alive, 0.1f, 1f, 0.05f, 0.25f);
                    scale = new Vector3(scale.x + s, scale.y + s, scale.z + s);
        
                    var p = dnData.position1;

                    p -= new float3(_dirs[i % _dirs.Length].x, _dirs[i % _dirs.Length].y, 0) * alive * 0.15f;

                    if(_m.Length < _m.Capacity) {
                        _m.AddNoResize(Matrix4x4.TRS(p, quaternion.identity, scale));
                        _d.AddNoResize(new Vector4(dnData.value1, dnData.value2, dnData.value3, dnData.value4) * 0.1f);
                        _a.AddNoResize(alpha);
                        _t.AddNoResize(0.05f);
                        shouldDrawGlobal = true;
                    }
                }
            }).Run();

        if(shouldDrawGlobal) {
            var drawnSoFar = 0;
            var repeat = math.floor(_m.Length / cap) + 1;
            //Debug.Log($"Repeat: {repeat}");
            for(int i = 0; i < repeat; i++) {
                CopyData(ref drawnSoFar, ref _m, ref _a, ref _d, ref _t);
                Draw();
            }

            //Debug.Log($"Should have drawn {_m.Length} meshes.");
        }

        _d.Dispose();
        _a.Dispose();
        _m.Dispose();
        _t.Dispose();

        var count = timers.Length;
        for(var i = 0; i < count; i++) {
            if(timers[i] > 0) {
                timers[i] -= 1 * UnityEngine.Time.deltaTime;
                if(timers[i] <= 0) {
                    timers[i] = 0;
                    matrix[i] = default;
                    alphas[i] = default;
                    digits[i] = default;
                    //Debug.Log("Reset");
                }
            }
        }
    }

    void CopyData(ref int drawnSoFar, ref NativeList<Matrix4x4> matrices, ref NativeList<float> alphas, ref NativeList<Vector4> digits, ref NativeList<float> timers) {
        if(matrices.Length > 0) { // All three native lists _should_ have the same Lengths.
            var amount = math.min(cap, matrices.Length - drawnSoFar);
            //Debug.Log($"amount:{amount}, drawnSoFar:{drawnsoFar}, matrices.Length:{matrices.Length}");
            for(int i = 0; i < amount; i++) {
                matrix[i] = matrices[i + drawnSoFar];
                this.alphas[i] = alphas[i + drawnSoFar];
                this.digits[i] = digits[i + drawnSoFar];
                this.timers[i] = timers[i + drawnSoFar];
            }
            drawnSoFar += amount;
        }
    }

    void Draw() {
        if(matrix.Length > 0) {
            mpb.SetFloatArray(color_prop, alphas);
            mpb.SetVectorArray(digit_prop, digits);
            Graphics.DrawMeshInstanced(pointMesh, 0, material, matrix, matrix.Length, mpb);
        }
    }

    static void GenerateArcDirections(ref NativeArray<Vector2> directions) {
        var numDirections = directions.Length;
        float angleIncrement = math.PI / (numDirections - 1);
        float currentAngle = math.PI;  // Starting from the left direction.

        for(int i = 0; i < numDirections; i++) {
            float x = math.cos(currentAngle);
            float y = math.sin(currentAngle);
            directions[i] = new Vector2(x, y);

            currentAngle += angleIncrement;
        }
    }

    static float Remap(float x, float xMin, float xMax, float newMin, float newMax) {
        x = math.max(xMin, math.min(xMax, x));
        var normalizedX = (x - xMin) / (xMax - xMin);
        var remappedValue = newMin + normalizedX * (newMax - newMin);
        return remappedValue;
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(DrawDamageNumberMeshSystem))]
public partial class DamageNumberUpdateTimer : SystemBase {
    protected override void OnUpdate() {
        var playerMove = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;
        var dt = UnityEngine.Time.deltaTime;

        Entities
            .WithName("UpdatePositionAndTimer")
            .ForEach((ref DynamicBuffer<DamageNumberData> buffer) => {
                var length = buffer.Length;
                for(int i = 0; i < length; i++) {
                    var dn = buffer[i];
                    if(!dn.shouldDraw) continue;

                    dn.position1 -= playerMove;

                    dn.timeAlive += dt;

                    if(dn.timeAlive >= 1f) {
                        dn.timeAlive = 0;
                        dn.shouldDraw = false;
                        dn.value1 = dn.value2 = dn.value3 = dn.value4 = 0;
                        dn.position1 = float3.zero;
                    }
                    buffer[i] = dn;
                }
            }).ScheduleParallel();
    }
}