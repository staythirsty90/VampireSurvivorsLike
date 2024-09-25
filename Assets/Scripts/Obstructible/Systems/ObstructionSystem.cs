using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using sc = Unity.Physics.SphereCollider;

public partial class ObstructionSystem : SystemBase {
    bool spawned;
    StageManager StageManager;

    protected override void OnStartRunning() {
        base.OnStartRunning();
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        UnityEngine.Debug.Assert(StageManager != null);
    }

    protected unsafe override void OnUpdate() {
        var max = ScreenBounds.Outer_Max_XY;

        if(!spawned) {
            spawned = true;
            var obstpositions = new NativeList<float3>(Allocator.TempJob);
            var obstradius = new NativeList<float>(Allocator.TempJob);
            var obstents = new NativeList<Entity>(Allocator.TempJob);

            Entities
                .WithAll<Obstructible>()
                .ForEach((Entity e, ref State state, in LocalTransform ltf, in PhysicsCollider pc) => {
                    state.isActive = true;
                    obstpositions.Add(ltf.Position);
                    var radius = ((sc*)pc.ColliderPtr)->Radius;
                    obstradius.Add(radius);
                    obstents.Add(e);
                }).Run();

            var obstsHashSet = new NativeHashSet<Entity>(1000, Allocator.TempJob);

            Job
                .WithCode(() => {
                    var AllObstructibles = GetComponentLookup<Obstructible>(true);
                    var AllStates = GetComponentLookup<State>();
                    var AllTranslations = GetComponentLookup<LocalTransform>();
                    CheckOverlap(ref obstpositions, ref obstpositions, ref obstradius, ref obstradius, ref obstents, ref obstsHashSet);
                    MoveOverlaps(ref obstsHashSet, ref AllStates, ref AllTranslations, -580);
                }).Run();

            obstpositions.Dispose();
            obstradius.Dispose();
            obstents.Dispose();
            obstsHashSet.Dispose();
        }

        var playerMove = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;

        if(playerMove.x == 0 && playerMove.y == 0)
            return;

        var rect        = StageManager.CurrentStage.Rect;
        var rightEdge   = rect.xMax;
        var topEdge     = rect.yMax;
        var leftEdge    = -rightEdge;
        var botEdge     = -topEdge;

        Entities
            .ForEach((Entity e, ref LocalTransform position, in State state, in Obstructible ob) => {
                if(!state.isActive) return;

                var pos = position.Position;
                
                if((ob.flags & Obstructible.Flags.DoNotWrapX) == 0) {
                    if(playerMove.x < 0) {
                        if(pos.x > rightEdge) {
                            pos.x = leftEdge + (pos.x - rightEdge);
                        }
                    }
                    else if(playerMove.x > 0) {
                        if(pos.x < leftEdge) {
                            pos.x = rightEdge + (pos.x - leftEdge);
                        }
                    }
                }

                if((ob.flags & Obstructible.Flags.DoNotWrapY) == 0) {
                    if(playerMove.y < 0) {
                        if(pos.y > topEdge) {
                            pos.y = botEdge + (pos.y - topEdge);
                        }
                    }
                    else if(playerMove.y > 0) {
                        if(pos.y < botEdge) {
                            pos.y = topEdge + (pos.y - botEdge);
                        }
                    }
                }

                position.Position = pos;

            }).ScheduleParallel();

        //
        // TODO: Dealing with the Inner and Outer Rects probably doesn't belong here.
        //

        var blockerPositions = new NativeArray<float>(2, Allocator.TempJob);

        Entities
            .WithAll<ObstructibleBlockerTag>()
            .ForEach((in LocalTransform ltf, in BoundingBox bb) => {
                if(ltf.Position.y > 0) { // assuming this is the top blocker.
                    blockerPositions[0] = bb.min.y;
                }
                else { // else assuming this is the bottom blocker.
                    blockerPositions[1] = bb.max.y;
                }
            }).Schedule();

        Job
            .WithName("MoveInnerAndOuterRects")
            .WithReadOnly(blockerPositions)
            .WithDisposeOnCompletion(blockerPositions)
            .WithCode(() => {

                var innerRect = ScreenBounds.InnerRect;
                var outerRect = ScreenBounds.OuterRect;

                var topY      = blockerPositions[0];
                var botY      = blockerPositions[1];
                var center      = float2.zero;

                if(topY == 0) {
                    return; // Assuming there is no top blocker, returning.
                }

                innerRect.center = center;
                outerRect.center = center;

                if(topY < innerRect.yMax) {
                    center.y -= innerRect.yMax - topY;
                }
                else if(botY > innerRect.yMin) {
                    center.y -= innerRect.yMin - botY;
                }

                ScreenBounds.InnerRect.center = center;
                ScreenBounds.OuterRect.center = center;
            }
        ).WithoutBurst().Run();
    }

    private static void MoveOverlaps(ref NativeHashSet<Entity> HashSet, ref ComponentLookup<State> AllStates, ref ComponentLookup<LocalTransform> AllTranslations, float xOffset) {
        foreach(var item in HashSet) {
            var trans = AllTranslations[item];
            var state = AllStates[item];
            trans.Position = new float3(-xOffset, item.Index, 0);
            state.isActive = false;
            AllTranslations[item] = trans;
            AllStates[item] = state;
            //UnityEngine.Debug.Log($"Moving Overlap, Entity({ item.Index }:{item.Version})");
        }
    }

    private static void CheckOverlap(ref NativeList<float3> positionsA, ref NativeList<float3> positionsB, ref NativeList<float> radiusA, ref NativeList<float> radiusB, ref NativeList<Entity> entities, ref NativeHashSet<Entity> hashset) {
        for(int i = 0; i < positionsA.Length; i++) {
            for(int j = 0; j < positionsB.Length; j++) {
                if(entities[i].Index == entities[j].Index) continue;
                if(math.distance(positionsA[i], positionsB[j]) <= radiusA[i] + radiusB[j]) {
                    if(!hashset.Contains(entities[j])) {
                        // overlap
                        hashset.Add(entities[j]);
                    }
                }
            }
        }
    }
}