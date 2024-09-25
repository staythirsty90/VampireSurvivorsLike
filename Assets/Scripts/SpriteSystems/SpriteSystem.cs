using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

/// <summary>
/// Updates the BoundingBox components.
/// TODO: Fix the calculation of the center so that it works for all sprites regardless of their pivots or size.
/// </summary>
[UpdateInGroup(typeof(Init3))]
public partial class UpdateSpriteBounds : SystemBase {
    protected override void OnUpdate() {
        var playerMoveDelta = SystemAPI.GetSingleton<FrameStatsSingleton>().playerMoveDelta;
        var spriteSizes = SpriteDB.Instance.SpriteSizeHashMap;

        Entities
            .WithReadOnly(spriteSizes)
            .WithNone<Missile>()
            .ForEach((ref BoundingBox bb, in State state, in SpriteFrameData sf_data, in Gfx gfx, in LocalTransform ltf, in NonUniformScale nuScale) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                var name = sf_data.frameCount > 1 ? $"{gfx.spriteName}_{sf_data.currentFrame + gfx.startingFrame}" : gfx.spriteName;
                var spriteData = spriteSizes[name];
                var nuscalePositive = math.abs(nuScale.Value);
                var size = spriteData.size * nuscalePositive * ltf.Scale;
                var center = ltf.Position;
                center.y += spriteData.center.y * 0.5f; // TODO: Hacky calculation.
                bb.SetBounds(center, size);
            }).ScheduleParallel();

        Entities
            .WithReadOnly(spriteSizes)
            .WithNone<Missile>()
            .ForEach((ref BoundingBox bb, in State state, in Gfx gfx, in LocalTransform ltf, in NonUniformScale nuScale) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                var name = gfx.spriteName;
                var spriteData = spriteSizes[name];
                var nuscalePositive = math.abs(nuScale.Value);
                var size = spriteData.size * nuscalePositive * ltf.Scale;
                var center = ltf.Position;
                center.y += spriteData.center.y * 0.5f; // TODO: Hacky calculation.
                bb.SetBounds(center, bb.size);
            }).ScheduleParallel();

        Entities
            .WithName("Missiles")
            .WithReadOnly(spriteSizes)
            .ForEach((ref BoundingBox bb, in Missile missile, in LocalTransform ltf, in State state, in SpriteFrameData sf_data, in Gfx gfx, in NonUniformScale nuScale) => {
                if(!state.isActive && !state.needsSpriteAndParticleSystem) return;
                if(state.isDying) return;
                var name = sf_data.frameCount > 1 ? $"{gfx.spriteName}_{sf_data.currentFrame}" : gfx.spriteName;
                var nuscalePositive = math.abs(nuScale.Value);
                var spriteSize = spriteSizes[name].size;
                var size = spriteSize * nuscalePositive * ltf.Scale;
                bb.SetBounds(ltf.Position, size, nuScale.Value.x == -1, bb.rotationAngle);
            }).ScheduleParallel();
    }
}

/// <summary>
/// Updates the SpriteFrameData components.
/// </summary>
public partial class SpriteFramesSystem : SystemBase {
    protected override void OnUpdate() {
        var dt = UnityEngine.Time.deltaTime;
        var fs = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var delta = fs.playerMoveDelta;
        var magnitude = math.max(0.3f, Vector3.SqrMagnitude(delta));
        var isMoving = delta.x > 0 || delta.x < 0 || delta.y > 0 || delta.y < 0;

        Entities
            .ForEach((ref SpriteFrameData sf_data, in State state, in Missile missile) => {
                if(!sf_data.autoUpdate) return;
                if(Utils.IsLocked(state.StatusEffects)) {
                    return;
                }
                if(!state.isActive) return;

                if((missile.Flags & MissileFlags.KillAfterAnimation) != 0) {
                    if(sf_data.loopCount > 0) return;
                }
                sf_data.needsSpriteUpdated = false;

                sf_data.frameTimer += dt;
                var count = sf_data.frameCount == 0 ? 1 : sf_data.frameCount;
                while(sf_data.frameTimer >= sf_data.frameTimerMax) {
                    sf_data.frameTimer -= sf_data.frameTimerMax;
                    sf_data.currentFrame = (sf_data.currentFrame + 1) % count;
                    sf_data.needsSpriteUpdated = true;
                    if(sf_data.currentFrame == 0) {
                        sf_data.loopCount += 1;
                        if((missile.Flags & MissileFlags.KillAfterAnimation) != 0) {
                            sf_data.currentFrame = sf_data.frameCount - 1;
                        }
                    }
                }
            }).ScheduleParallel();

        Entities
            .WithNone<Missile>()
            .WithNone<PlayerAnimation>()
            .ForEach((ref SpriteFrameData sf_data, in State state) => {
                if(!sf_data.autoUpdate) return;
                if(Utils.IsLocked(state.StatusEffects)) {
                    return;
                }
                if(!state.isActive) return;
                sf_data.needsSpriteUpdated = false;
                sf_data.frameTimer += dt;
                var count = sf_data.frameCount == 0 ? 1 : sf_data.frameCount;
                while(sf_data.frameTimer >= sf_data.frameTimerMax) {
                    sf_data.frameTimer -= sf_data.frameTimerMax;
                    sf_data.currentFrame = (sf_data.currentFrame + 1) % count;
                    sf_data.needsSpriteUpdated = true;
                    if(sf_data.currentFrame == 0) {
                        sf_data.loopCount += 1;
                    }
                }
            }).ScheduleParallel();

        // TODO: This is ugly hack to have player only animate if its moving.
        Entities.ForEach((ref SpriteFrameData sf_data, in PlayerAnimation player) => {
            if(sf_data.autoUpdate) {
                sf_data.needsSpriteUpdated = false;
                sf_data.frameTimer += dt;
                sf_data.frameCount = isMoving ? player.SpriteNames_Moving.Length : player.SpriteNames_Idle.Length;
                var count = sf_data.frameCount == 0 ? 1 : sf_data.frameCount;
                while(sf_data.frameTimer >= sf_data.frameTimerMax) {
                    sf_data.frameTimer -= sf_data.frameTimerMax;
                    sf_data.currentFrame = (sf_data.currentFrame + 1) % count;
                    sf_data.needsSpriteUpdated = true;
                }
            }
        }).ScheduleParallel();
    }
}

/// <summary>
/// Sets the Non Uniform Scale values of entities that should "Flip".
/// </summary>

[UpdateInGroup(typeof(LateSimulationSystemGroup))] // Have to update after Companion G.O. Update Transform System.
public partial class FlipSpriteSystem : SystemBase {

    protected override void OnUpdate() {
        var playerPos = float3.zero;

        Entities
            .WithAll<Enemy>()
            .WithNone<Destructible>()
            .ForEach((ref NonUniformScale nuScale, in Movement movement, in LocalTransform ltf, in State state) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                if(Utils.IsLocked(state.StatusEffects)) return;
                nuScale.Value.x *= math.sign(nuScale.Value.x) * movement.Direction.x > 0 ? -1 : 1;
            }).ScheduleParallel();

        Entities
            .ForEach((ref NonUniformScale nuScale, in Missile missile, in LocalTransform ltf, in State state) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                if((missile.Flags & MissileFlags.FacesCenter) == 0) return;
                nuScale.Value.x *= math.sign(nuScale.Value.x) * ltf.Position.x > 0 ? -1 : 1;
            }).ScheduleParallel();
    }
}

/// <summary>
/// Updates the SpriteRenderer's on the Companion GameObjects
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SpriteSystem : SystemBase {
    protected override void OnUpdate() {
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var isPlayerFacingRight = frameStats.isPlayerFacingRight;
        var playerMoveDelta = frameStats.playerMoveDelta;
        var isPlayerMoving = playerMoveDelta.x > 0 || playerMoveDelta.x < 0 || playerMoveDelta.y > 0 || playerMoveDelta.y < 0;
        var playerPos = float3.zero;

        // Animate player.
        {
            Entities.ForEach((SpriteRenderer sr, in PlayerAnimation player, in SpriteFrameData sfdata) => {
                sr.flipX = !isPlayerFacingRight;

                if(isPlayerMoving)
                    sr.sprite = SpriteDB.Instance.Get(player.SpriteNames_Moving[sfdata.currentFrame]);
                else
                    sr.sprite = SpriteDB.Instance.Get(player.SpriteNames_Idle[sfdata.currentFrame]);

            }).WithoutBurst().Run();
        }

        // Update sprites for pickups.
        {
            Entities.ForEach((SpriteRenderer sr, ParticleSystem ps, ref State state, ref Gfx gfx, in PickUp pickup, in ID id) => {
                if(state.needsSpriteAndParticleSystem) {
                    state.needsSpriteAndParticleSystem = false;
                    state.isActive = true;
                    if(gfx.spriteName.Length == 0) return;

                    if(id.Guid == PickUps.XpGemID.Guid) {
                        //Debug.Log($"xp value: {pickup.value}");
                        var name = "bluegem";
                        if(pickup.value >= 2 && pickup.value <= 30) name = "greengem";
                        else if(pickup.value > 30) name = "redgem";
                        sr.sprite = SpriteDB.Instance.Get(name);
                        gfx.spriteName = name;
                    }
                    else sr.sprite = SpriteDB.Instance.Get(gfx.spriteName);
                    ParticleSystemHelper.SetParticleSystem(id.Guid, ps);
                }
            }).WithoutBurst().Run();
        }

        // Update Enemy sprites.
        // NOTE: Only Enemies use needsRendererUpdated.
        {
            Entities
                .ForEach((EnemyAspect enemy, SpriteRenderer sr, ref SpriteFrameData sf_data) => {
                    if(sf_data.needsRendererUpdated && !enemy.State.ValueRO.isActive) {
                        var gfx = Enemies.Table[enemy.Id.ValueRO.Guid].gfx;
                        enemy.Gfx.ValueRW = gfx;
                        (sr.sprite, sf_data.frameCount) = SpriteDB.Instance.Get(enemy.Id.ValueRO.Guid);
                        sf_data.currentFrame = UnityEngine.Random.Range(0, sf_data.frameCount);
                        sf_data.loopCount = 0;
                        sf_data.needsRendererUpdated = false;
                        enemy.State.ValueRW.readyToActivate = true;
                    }
                    else if(enemy.State.ValueRO.isActive && sf_data.needsSpriteUpdated && sf_data.frameCount > 1) {
                        sr.sprite = SpriteDB.Instance.Get(enemy.Id.ValueRO.Guid, sf_data.currentFrame);
                    }

                }).WithoutBurst().Run();
        }

        // Update active sprite renderers that have multiple frames.
        Entities
            .WithNone<Enemy>()
            .ForEach((SpriteRenderer sr, in SpriteFrameData sf_data, in State state, in ID id) => {
                if(state.isActive && sf_data.needsSpriteUpdated && sf_data.frameCount > 1) {
                    sr.sprite = SpriteDB.Instance.Get(id.Guid, sf_data.currentFrame);
                }
            }).WithoutBurst().Run();
    }
}