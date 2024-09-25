using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class PickUpCollectSystem : SystemBase {
    public ParticleSystem gemPickupParticleSystem;
    protected override void OnCreate() {
        base.OnCreate();
        gemPickupParticleSystem = Resources.Load<ParticleSystem>("ParticleSystems/Particle System GemPickup");
        Debug.Assert(gemPickupParticleSystem, "Couldn't load Particle System GemPickup");
    }

    protected override void OnUpdate() {
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        var gemSpeed  = chara.CharacterStats.Get(Stats.PickUpGrabSpeedID).value;
        var grabRange = chara.CharacterStats.Get(Stats.PickUpGrabRangeID).value;
        var moveSpeed = chara.CharacterStats.Get(Stats.MovementSpeedID).value;
        var xpGrowth  = chara.CharacterStats.Get(Stats.GrowthID).value;
        var playerPos = float3.zero;
        var time = UnityEngine.Time.time;
        var dt = UnityEngine.Time.deltaTime;
        var MinimumDistanceAfterSwoop = 0.35f;
        var xpgem_id = PickUps.XpGemID;

        if(math.any(frameStats.playerMoveDelta)) {
            Entities.ForEach((ref Swoops swoops, in LocalTransform position, in State state, in PickUp pickUp, in ID id) => {
                if(!state.isActive) return;
                if(swoops.swooping) return;
                if(swoops.grabbed) return;
                var distance = math.distance(position.Position, playerPos);
                if(grabRange >= distance) {
                    swoops.swooping = true;
                    var direction = position.Position - playerPos;
                    swoops.swoopDirection = direction;
                    swoops.swoopStartPosition = position.Position;
                    swoops.swoopStartTime = time;
                    if(id.Guid == xpgem_id.Guid) {
                        // TODO: Move this into another system so we can burst/schedule this function.
                        gemPickupParticleSystem.transform.position = position.Position;
                        gemPickupParticleSystem.Emit(1);
                    }
                }
            }).WithoutBurst().Run();
        }

        Dependency = Entities.ForEach((Entity entity, ref Swoops swoops, ref LocalTransform position, in State state, in PickUp pickUp) => {
            if(!state.isActive) return;
            if(!swoops.swooping) return;
            if(swoops.grabbed) return;
            //Debug.Log($"swoopStartPos: {swoops.swoopStartPosition}, swoopDir: {swoops.swoopDirection}, swoopStartTime: {swoops.swoopStartTime}, swoopDistance: {swoops.distance}, grabbed: {swoops.grabbed}, swooping: {swoops.swooping}");
            var end = swoops.swoopStartPosition + (math.normalize(swoops.swoopDirection) /*+ pickUpSwoopDistance*/);
            end.z = 0;
            var distanceCovered = (time - swoops.swoopStartTime) * gemSpeed;
            // TODO: "pickup distance" is more like duration. the lower the 'distance' the faster the gems swoops but it always moves the same world distance. why??
            if(swoops.distance <= 0) {
                swoops.distance = 0.3f;
            }
            var frac = distanceCovered / swoops.distance;
            var pos = math.lerp(swoops.swoopStartPosition, end, frac);
            position.Position = pos;
            if(frac >= 1f) {
                swoops.swooping = false;
                swoops.grabbed = true;
                swoops.swoopDirection = float3.zero;
            }
        }).ScheduleParallel(Dependency);

        var ID_Q = new NativeQueue<ID>(Allocator.TempJob);
        var pickupQ = new NativeQueue<PickUp>(Allocator.TempJob);

        var gold = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GoldCollectedSystem>().GoldCollectedThisFrame;

        Dependency = Entities.WithName("PickUpMovement").ForEach((Entity entity, ref PickUp pickUp, ref Swoops swoops, ref LocalTransform position, ref State state, in ID id) => {
            if(!state.isActive) return;
            if(!swoops.grabbed) return;

            var distance = math.distancesq(position.Position, playerPos);
            if(MinimumDistanceAfterSwoop >= distance) {
                // the pickup finally reaches the player's position
                state.isActive = false;
                swoops.grabbed = false;
                swoops.swoopStartTime = 0;
                swoops.swoopStartPosition = float3.zero;
                position.Position = new float3(-70, -2 * entity.Index, 0);
                pickupQ.Enqueue(pickUp);
                ID_Q.Enqueue(id);
            }
            else { // move the pickup towards the player's position
                var pos = playerPos;
                pos.y += 0.3f;
                var direction = math.normalize(pos - position.Position);
                position.Position += math.max(moveSpeed + 1, gemSpeed) * dt * direction;
                position.Position.z = 0;
            }
        }).Schedule(Dependency);

        Dependency = Entities
            .WithDisposeOnCompletion(ID_Q)
            .WithDisposeOnCompletion(pickupQ)
            .ForEach((ref FrameStatsSingleton frameStats) => {
                if(ID_Q.Count != 0) {
                    gold[0] = 0;

                    while(ID_Q.Count > 0) {
                        var id = ID_Q.Dequeue().Guid;
                        var pickup = pickupQ.Dequeue();

                        switch(pickup.Class) {
                            case PickUpClass.Gold:
                                gold[0] += (uint)pickup.value;
                                break;
                            case PickUpClass.Magnet:
                                frameStats.collectedMagnet = true;
                                break;
                            case PickUpClass.Heal:
                                frameStats.totalHealed += pickup.value;
                                break;
                            case PickUpClass.Rosary:
                                frameStats.collectedRosary = true;
                                break;
                        }

                        if (id == xpgem_id.Guid) {
                            frameStats.expGained += pickup.value + (pickup.value * xpGrowth);
                        }
                        if(!pickup.GivePowerUpName.IsEmpty) {
                            frameStats.powerUpNameToGiveToPlayer.Add(pickup.GivePowerUpName);
                        }
                        if(pickup.StatIncrease.statType != Guid.Empty) {
                            frameStats.statIncreasesToGiveToPlayer.Add(pickup.StatIncrease);
                        }
                        else {
                            //Debug.Log($"pickup stat increase was empty. statType: {pickup.StatIncrease.statType}, value: {pickup.StatIncrease.value}");
                        }
                    }
                }
            }).Schedule(Dependency);
    }
}