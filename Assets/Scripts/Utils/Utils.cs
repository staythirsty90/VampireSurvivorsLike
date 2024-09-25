using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;

public static class Utils {

    public static float2 GetFloorXYBounds() {
        var rowscols = GetFloorRowsAndCols();

        var x = rowscols.x * 0.5f + 0.5f;
        var y = rowscols.y * 0.5f + 0.5f;

        return new float2(x, y);
    }

    public static float2 GetFloorRowsAndCols(uint extraBorder = 3) {
        var ppu = 48;
        var x = math.ceil(Screen.width  / ppu) + extraBorder;
        var y = math.ceil(Screen.height / ppu) + extraBorder;
        return new float2(x, y);
    }

    public static bool IsLocked(in FixedList128Bytes<StatusEffectApplier> buffer) {
        for(int i = 0; i < buffer.Length; i++) {
            var effect = buffer[i];
            if(effect.Duration > 0 && (effect.HitEffect == HitEffect.Knockback || effect.HitEffect == HitEffect.Freeze)) return true;
        }
        return false;
    }

    public static bool IsFrozen(in FixedList128Bytes<StatusEffectApplier> buffer) {
        for(int i = 0; i < buffer.Length; i++) {
            var effect = buffer[i];
            if(effect.Duration > 0 && effect.HitEffect == HitEffect.Freeze) return true;
        }
        return false;
    }

    public static bool IsKnockedBack(in FixedList128Bytes<StatusEffectApplier> buffer) {
        for(int i = 0; i < buffer.Length; i++) {
            var effect = buffer[i];
            if(effect.Duration > 0 && effect.HitEffect == HitEffect.Knockback) return true;
        }
        return false;
    }

    public static void RemoveKnockBack(ref FixedList128Bytes<StatusEffectApplier> buffer) {
        for(int i = 0; i < buffer.Length; i++) {
            var effect = buffer[i];
            if(effect.HitEffect == HitEffect.Knockback) {
                effect.Duration = 0;
                buffer[i] = effect;
                break;
            }

        }
    }

    public static float3 GetKnockBack(in FixedList128Bytes<StatusEffectApplier> buffer) {
        for(int i = 0; i < buffer.Length; i++) {
            var effect = buffer[i];
            if(effect.HitEffect == HitEffect.Knockback) {
                //Debug.Log($"found knockback direction: {effect.knockBackDirection}");
                return new float3(effect.knockBackDirection, effect.Duration);
            }
        }
        return float3.zero;
    }

    public static void AddStatusEffect(ref StatusEffectApplier effect, ref FixedList128Bytes<StatusEffectApplier> enemyStatus, in float3 enemyPos, in float3 missilePos) {
        for(int i = 0; i < enemyStatus.Length; i++) {
            var es = enemyStatus[i];
            if(es.HitEffect == effect.HitEffect) {
                es.Duration = effect.Duration;
                if(es.HitEffect == HitEffect.Knockback && es.Duration > 0) {
                    //es.knockBackDirection = math.normalizesafe(enemyPos.xy - missilePos.xy);
                    es.knockBackDirection = math.normalizesafe(enemyPos.xy - float2.zero);
                    //Debug.Log($"Setting knockback direction: {es.knockBackDirection}");
                }
                enemyStatus[i] = es;
                break;
            }
        }
    }

    public static bool IsOverlappingBox2D(ref BoundingBox box1, ref BoundingBox box2) {
        return
        box1.max.x >= box2.min.x && box2.max.x >= box1.min.x // Is Overlapping in 1D X ?
        &&
        box1.max.y >= box2.min.y && box2.max.y >= box1.min.y;// Is Overlapping in 1D Y ?
    }

    public static bool IsCircleOverlappingBox2D(in BoundingBox box, in float2 circleCenter, in float circleRadius) {
        // Calculate the closest point on the rectangle to the circle.
        var closestX = math.clamp(circleCenter.x, box.min.x, box.max.x);
        var closestY = math.clamp(circleCenter.y, box.min.y, box.max.y);
        // Calculate the distance between the circle's center and the closest point on the rectangle.
        var distance = math.distance(new float2(circleCenter.x, circleCenter.y), new float2(closestX, closestY));
        // Check if the distance is less than or equal to the circle's radius.
        //Debug.Log($"overlapping: {distance <= circleRadius}, closest:({closestX},{closestY}), dist: {distance}, circleCenter: ({circleCenter.x},{circleCenter.y}), radius: {circleRadius}");
        return distance <= circleRadius;
    }

    public static (bool, float) IsCircleOverlappingBox2D(in float3 boxCenter, float3 boxSize, in float2 circleCenter, in float circleRadius) {
        var boxMin = new float3 (boxCenter.x - boxSize.x * 0.5f, boxCenter.y - boxSize.y * 0.5f, boxCenter.z);
        var boxMax = new float3 (boxCenter.x + boxSize.x * 0.5f, boxCenter.y + boxSize.y * 0.5f, boxCenter.z);
        var closestX = math.clamp(circleCenter.x, boxMin.x, boxMax.x);
        var closestY = math.clamp(circleCenter.y, boxMin.y, boxMax.y);
        var distance = math.distance(new float2(circleCenter.x, circleCenter.y), new float2(closestX, closestY));
        //Debug.Log($"closest:{closestX}, {closestY}, dist: {distance}, circleCenter: {circleCenter.x}, {circleCenter.y}, radius: {circleRadius}");
        return (distance <= circleRadius, distance);
    }

    public static bool IsCircleOverlappingRotatedBox2D(in BoundingBox box, in float2 circleCenter, in float circleRadius) {

        var botLeft  = box.min;
        var topRight = box.max;

        var rotationAngle = box.rotationAngle;
            var cosTheta = math.cos(math.radians(-rotationAngle));
            var sinTheta = math.sin(math.radians(-rotationAngle));

            var center = box.pivot;

            // Transform to Local space.
            botLeft  -= center;
            topRight -= center;

            var cx = circleCenter.x - center.x;
            var cy = circleCenter.y - center.y;
        
            var localCircleX = cx * cosTheta - cy * sinTheta;
            var localCircleY = cx * sinTheta + cy * cosTheta;

            var closestX = math.clamp(localCircleX, botLeft.x, topRight.x);
            var closestY = math.clamp(localCircleY, botLeft.y, topRight.y);

            var distance = math.distance(new float2(localCircleX, localCircleY), new float2(closestX, closestY));
            return distance <= circleRadius;
    }

    public static (bool, float) IsCircleOverlappingCircle(in float2 circleCenter, in float circleRadius, in float2 circleCenter2, in float circleRadius2) {
        var distance = math.distance(circleCenter, circleCenter2);
        return (distance <= circleRadius + circleRadius2, distance);
    }
    
    unsafe public static float GetRadius(in Unity.Physics.Collider* cptr) {
        if(cptr->Type == ColliderType.Sphere) {
            var sphere = (Unity.Physics.SphereCollider*)cptr;
            return sphere->Geometry.Radius;
        }
        // TODO: Either return a "radius" for other collider types or make functions for them.
        return 0.5f;
    }

    unsafe public static float3 GetCenter(in Unity.Physics.Collider* cptr) {
        var sphere = (Unity.Physics.SphereCollider*)cptr;
        return sphere->Geometry.Center;
    }
}