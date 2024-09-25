using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TODO: Robustness. There are lots of hard coded magic numbers that may break as the project changes.
/// </summary>

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DrawTreasureIndicator : SystemBase {
    readonly List<SpriteRenderer> list = new(10);
    const float frameDuration = 0.1f;
    float index = 0;
    int dir = 1;
    float _t;
    protected override void OnCreate() {
        var parent = new GameObject("Treasure Indicators");
        for(int i = 0; i < 10; i++) {
            var sr = new GameObject($"Treasure Indicator {i}").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent.transform);
            var sprite = SpriteDB.Instance.Get("indicator_0");
            sr.sprite = sprite;
            sr.transform.position = new Vector3(-100, -100 - (1 * i), 0);
            sr.gameObject.SetActive(false);
            sr.sortingOrder = 100;
            list.Add(sr);
        }
    }

    protected override void OnUpdate() {
        int activeTreasures = 0;
        var positions = new NativeList<float3>(10, Allocator.TempJob);
        var xmax = ScreenBounds.X_Max;
        var ymax = ScreenBounds.Y_Max;
        var size = new float3(0.05f, -0.5f, 0);
        var xoff = 0f;
        var yoff = 0.2f;

        Entities.ForEach((in State state, in LocalTransform ltf, in Treasure treasure, in BoundingBox bb) => {
            if(!state.isActive) return;
            var pos = ltf.Position;
            pos.y += bb.size.y * 0.75f;
            activeTreasures++;
            positions.Add(pos);
        }).Run();

        _t += UnityEngine.Time.deltaTime;

        if(_t >= frameDuration) {
            _t -= frameDuration; // Reset the timer
            index = (index + dir) % 4; // Use modular arithmetic to loop between 0 and 3

            if(index == 3 || index == 0) {
                dir *= -1; // Reverse direction when reaching the ends
            }
        }

        var maxX = xmax - xoff;
        var maxY = ymax - yoff;

        for(int i = 0; i < activeTreasures; i++) {
            var item = list[i];
            item.gameObject.SetActive(true);
            var pos = positions[i];

            if(ScreenBounds.IsInView(pos, size, xmax, ymax)) {
                item.transform.SetPositionAndRotation(pos, quaternion.RotateZ(0));
                item.sprite = SpriteDB.Instance.Get($"indicator_{index}");
                continue;
            }

            pos.x = math.clamp(pos.x, -maxX, maxX);
            pos.y = math.clamp(pos.y, -maxY, maxY);

            var dir = float3.zero - pos;
            var angle = math.atan2(dir.x, dir.y);
            item.transform.SetPositionAndRotation(pos, quaternion.RotateZ(-angle));
            item.sprite = SpriteDB.Instance.Get($"indicator_{index}");
        }

        for(int i = activeTreasures; i < 10; i++) {
            var item = list[i];
            item.gameObject.SetActive(false);
        }
        positions.Dispose();
    }
}