using Unity.Mathematics;
using UnityEngine;

public readonly struct ScreenBounds {
    // TODO: Hard coded numbers.
    public readonly static float X_Max = 9.6f;
    public readonly static float Y_Max = 5.4f;
    public readonly static float2 Max_XY = new(X_Max, Y_Max);
    public readonly static float Outer_X_Max = X_Max * 1.3f;
    public readonly static float Outer_Y_Max = Y_Max * 1.5f;
    public readonly static float2 Outer_Max_XY = new(Outer_X_Max, Outer_Y_Max);
    
    public static Rect InnerRect = new(-X_Max, -Y_Max, X_Max * 2f, Y_Max * 2f);
    public static Rect OuterRect = new(-Outer_X_Max, -Outer_Y_Max, Outer_X_Max * 2f, Outer_Y_Max * 2f);

    public static void ResetRectCenters() {
        InnerRect.center = float2.zero;
        OuterRect.center = float2.zero;
    }

    public static float3 GetPositionOutOfSight(StageSpawnType spawnType, in Rect inner, in Rect outer, ref Unity.Mathematics.Random random, float3 spriteSize) {
        var pos = float3.zero;

        // TODO: Clamp the X and Y positions according to the sprite size.
        // Also, we might need to account for the sprite's pivot?

        switch(spawnType) {
            case StageSpawnType.AllFourSides: {

                switch(random.NextInt(0, 4)) {
                    case 0: { // bottom
                        pos.x = outer.x + random.NextFloat(0, inner.xMax - outer.x);
                        pos.y = outer.y + random.NextFloat(0, inner.yMin - outer.y) - spriteSize.y;
                    }
                    break;

                    case 1: { // top
                        pos.x = inner.x + random.NextFloat(0, outer.xMax - inner.x);
                        pos.y = inner.yMax + random.NextFloat(0, outer.yMax - inner.yMax);
                    }
                    break;

                    case 2: { // left
                        pos.x = outer.x + random.NextFloat(0, inner.x - outer.x);
                        pos.y = inner.y + random.NextFloat(0, outer.yMax - inner.y);
                    }
                    break;

                    case 3: { // right
                        pos.x = inner.xMax + random.NextFloat(0, outer.xMax - inner.xMax);
                        pos.y = outer.y + random.NextFloat(0, inner.yMax - outer.y);
                    }
                    break;
                }
                break;
            }
            
            case StageSpawnType.LeftAndRightSidesOnly: {
                switch(random.NextInt(0, 2)) {

                    case 0: { // left
                        pos.x = outer.x + random.NextFloat(0, inner.x - outer.x);
                        pos.y = inner.y + random.NextFloat(0, inner.yMax - inner.y);
                    }
                    break;

                    case 1: { // right
                        pos.x = inner.xMax + random.NextFloat(0, outer.xMax - inner.xMax);
                        pos.y = inner.y + random.NextFloat(0, inner.yMax - inner.y);
                    }
                    break;
                }
                // Clamp the Y here but we aren't accounting for the sprite pivot.
                pos.y = math.clamp(pos.y, inner.y, inner.yMax - spriteSize.y);
            }
            break;
        }
        return pos;
    }

    public static bool IsOutsideXY(float x, float y, in BoundingBox bb) {
        var rightEdge = x;
        var topEdge = y ;
        var leftEdge = -rightEdge;
        var bottomEdge = -topEdge;

        if(bb.min.x > rightEdge) {
            return true;
        }
        else if(bb.max.x < leftEdge) {
            return true;
        }

        if(bb.min.y > topEdge) {
            return true;
        }
        else if(bb.max.y < bottomEdge) {
            return true;
        }

        return false;
    }

    public static bool IsOutsideXY(float x, float y, in BoundingBox bb, in float3 ObstructibleWallPosition) {
        var rightEdge = x;
        var topEdge = y;
        var leftEdge = -rightEdge;
        var bottomEdge = -topEdge;

        if(bb.min.x > rightEdge) {
            return true;
        }
        else if(bb.max.x < leftEdge) {
            return true;
        }

        if(bb.min.y > topEdge) {
            return true;
        }
        else if(bb.max.y < bottomEdge) {
            return true;
        }

        return false;
    }

    public static bool IsInView(float3 pos, float3 size, float xmax, float ymax) {
        return pos.x <= xmax + size.x
            && pos.x >= -(xmax + size.x)
            && pos.y <= ymax + size.y
            && pos.y >= -(ymax + size.y);
    }
}