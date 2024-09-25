using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(Init1))]
[UpdateAfter(typeof(MissileSpawnSystem))] // can't merge this with MissileSystem because this needs to run after spawning the missiles and before missile collision
public partial class MissileUpdateStats : SystemBase {
    LootSystem LootSystem;
    StageManager StageManager;
    protected override void OnCreate() {
        base.OnCreate();
        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null);
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        Debug.Assert(StageManager != null);
    }

    protected override void OnUpdate() {
        var stageConfig     = StageManager.CurrentStage.configuration;
        var chara           = SystemAPI.GetSingleton<CharacterComponent>().character;
        var player_area     = chara.CharacterStats.Get(Stats.AreaID).value;
        var player_damage   = chara.CharacterStats.Get(Stats.MightID).value;
        var player_duration = chara.CharacterStats.Get(Stats.DurationID).value;
        var player_speed    = chara.CharacterStats.Get(Stats.SpeedID).value;

        var config_playerspeed = stageConfig.PlayerPxSpeed;
        var config_missilespeed = stageConfig.MissileSpeed;

        var AllMissiles = GetComponentLookup<Missile>(false);
        var AllStates   = GetComponentLookup<State>(true);
        var AllIDs      = GetComponentLookup<ID>(true);

        var mtable = Missiles.MissileTable;
        Unity.Mathematics.Random random = new((uint)UnityEngine.Random.Range(1, 100_000));

        Entities
            .WithName("MissileUpdateStats")
            .WithReadOnly(mtable)
            .WithReadOnly(AllIDs)
            .WithReadOnly(AllStates)
            .WithNativeDisableParallelForRestriction(AllMissiles)
            .ForEach((ref DynamicBuffer<WeaponMissileEntity> weaponsMissiles, in PowerUpComponent puc) => {
                var pu = puc.PowerUp;
                var powerup_area = pu.baseStats.Area;
                var powerup_speed = pu.baseStats.Speed;
                var powerup_damage = pu.baseStats.Damage;
                var powerup_duration = pu.baseStats.Duration;
                var powerup_weaponIndex = pu.weaponIndex;

                for(int i = 0; i < weaponsMissiles.Length; i++) {
                    var entity = weaponsMissiles[i].MissileEntity;
                    var state = AllStates[entity];
                    if(!state.isActive) {
                        //Debug.Log($"Skipping because State is InActive! ({entity.Index})");
                        continue;
                    }
                    if(state.isDying) {
                        //Debug.Log($"Skipping because State is Dying! ({entity.Index})");
                        continue;
                    }

                    var missile = AllMissiles[entity];
                    if((missile.Flags & MissileFlags.DontUpdateStats) != 0) {
                        //Debug.Log($"Skipping because Missile has DontUpdateStats ({entity.Index})");
                        continue;
                    }

                    if(powerup_weaponIndex != missile.weaponIndex) {
                        //Debug.Log($"Skipping because powerup index {powerup_weaponIndex} doesn't match missile weaponIndex {missile.weaponIndex} ({entity.Index})");
                        continue;
                    }

                    var id = AllIDs[entity];
                    var source = mtable[id.Guid].missile;
                    if((missile.Flags & MissileFlags.IgnoresArea) == 0) {
                        missile.Radius = GetBonus(player_area, powerup_area, source.Radius);
                        //Debug.Log($"new radius: {missile.Radius}, original radius: {source.Radius}, player_area:{player_area}, powerup_area:{powerup_area}");
                    }

                    if((missile.Flags & MissileFlags.IgnoresSpeed) == 0) {
                        missile.Speed = GetBonus(player_speed, powerup_speed, source.Speed, config_missilespeed) * 1920 / 683f;
                        //Debug.Log($"adjusting speed to: {missile.Speed} ({entity})");
                    }

                    if((missile.Flags & MissileFlags.IgnoresDuration) == 0) {
                        missile.Duration = GetBonus(player_duration, powerup_duration, source.Duration);
                        //Debug.Log($"missile duration: {source.Duration}, player_duration: {player_duration}, powerup_duration: {powerup_duration}, total missile Duration: {missile.Duration}");
                    }

                    if((missile.Flags & MissileFlags.SpeedIncreasesPerShot) != 0) {
                        missile.Speed += missile.firedIndex;
                    }

                    missile.Damage = source.Damage == 0 ? (ushort)0 : (ushort)(source.Damage + powerup_damage + (player_damage * random.NextFloat(0.8f, 1f)));

                    AllMissiles[entity] = missile;

                    //Debug.Log($"missile damage: {source.Damage}, player_damage:{player_damage}, powerup_damage:{powerup_damage}, total missile Damage: {missile.Damage}");    
                }
            }).ScheduleParallel();

        /*
         * We are searching through active missiles with a specific missile ID that corresponds to the PowerUp's missileID. 
         * If any such missiles are found, we update certain attributes like 'Area' and 'Damage'. 
         * This update occurs after the missiles are created because missiles may be persistent or immune to removal. 
         * In cases where the player's attributes like 'Area' are enhanced, these persistent missiles would not receive the updated attributes 
         * since we only apply the player's enhancements when the missile is spawned. This situation specifically affects persistent 
         * missiles, as they are spawned only once. This system ensures that the player's attribute enhancements are also applied 
         * to existing missiles that won't be spawned again.
         */
    }

    private static float GetBonus(float player_stat, float powerup_stat, float source, float configMultiplier = 1f) {
        float stat;
        if(source == 0) {
            stat = player_stat + powerup_stat;
        }
        else {
            // TODO: this math is not correct. 2 + ( 2 * ( 4 + 0) ) will return 10 (instead of 8) for player stat of 4, powerup stat of 0, and source of 2
            stat = source + (source * (player_stat + powerup_stat));
        }
        return stat * configMultiplier;
    }
}

[UpdateBefore(typeof(TransformSystemGroup))] // before Companion GO update
public partial class MissileSystem : SystemBase {

    LootSystem LootSystem;
    NativeArray<int> LootSystemArray;

    protected override void OnCreate() {
        base.OnCreate();
        LootSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<LootSystem>();
        Debug.Assert(LootSystem != null);
        LootSystemArray = new NativeArray<int>(128/*LootSystem.EquippedAll.Count*/, Allocator.Persistent); // TODO: EquippledAll.Count is 0 when the system is created.
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        LootSystemArray.Dispose();
    }

    protected override void OnUpdate() {
        var dt = UnityEngine.Time.deltaTime;
        var time = UnityEngine.Time.time;
        var frameStats = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var chara = SystemAPI.GetSingleton<CharacterComponent>().character;
        var playerArea = chara.CharacterStats.Get(Stats.AreaID).value; // TODO: Stats should be applied to the missile already.
        var random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100_000));
        var nullEnt = Entity.Null;
        var lootarray = LootSystemArray;
        var AllBuffers = GetBufferLookup<WeaponMissileEntity>(false);
        var AllPowerups = GetComponentLookup<PowerUpComponent>(false);

        // get the indices of powerups that were removed
        var resetIndices = new NativeList<int>(Allocator.TempJob);
        Entities
            .WithName("CheckForRemovedPowerUps")
            .ForEach((ref PowerUpComponent puc) => {
                var powerup = puc.PowerUp;
                var oldLevel = lootarray[powerup.weaponIndex];
                if(oldLevel > 0 && powerup.level == 0) { // flagged for removal
                    resetIndices.Add(powerup.weaponIndex);
                    powerup.familiarEntity = powerup.familiarTargetEntity = nullEnt;
                    Debug.Log($"removing {powerup.name}, index: {powerup.weaponIndex}");
                }
                lootarray[powerup.weaponIndex] = powerup.level;
                puc.PowerUp = powerup;
            }).Schedule();

        Entities
            .WithName("MissileSystem")
            .WithReadOnly(resetIndices)
            .WithDisposeOnCompletion(resetIndices)
            .ForEach((Entity entity, ref LocalTransform ltf, ref BoundingBox bb, ref Missile m, ref State state, ref SpriteFrameData sf_data, ref NonUniformScale nuScale, in ID id, in PowerUpLink pulink, in OffsetMovement movement) => {
                if(!state.isActive && !state.needsSpriteAndParticleSystem) return;

                if(state.isDying) { // dying

                    if(pulink.powerupComponent != nullEnt) {
                        // Remove dead missiles from a weapons missile list, otherwise the list will grow indefinitely.
                        var buffer = AllBuffers[pulink.powerupComponent];
                        if(buffer.Length > 0) {
                            for(var i = buffer.Length - 1; i > -1; i--) { // TODO: We can't use ScheduleParallel because we are accessing this buffer.
                                if(buffer[i].MissileEntity == entity && (state.isDying || (!state.isActive && state.minuteOfSpawn != -1))) {
                                    buffer.RemoveAt(i);
                                    //Debug.Log($"missile ({entity}) is dying: {state.isDying}, active: {state.isActive}");
                                    break;
                                }
                            }
                        }
                        // Reset the powerups familiar entity
                        var puc = AllPowerups[pulink.powerupComponent];
                        var powerup = puc.PowerUp;
                        if ((m.Flags & MissileFlags.IsFamiliar) != 0 && powerup.familiarEntity == entity) {
                            powerup.familiarEntity = nullEnt;
                            puc.PowerUp = powerup;
                            AllPowerups[pulink.powerupComponent] = puc;
                        }
                    }

                    if((m.Flags & MissileFlags.ShrinksOnDying) != 0) {
                        var f = time - state.timeOfDeath;
                        var finish = 0f;
                        ltf.Scale -= f * 5f * dt;
                        if(ltf.Scale / m.ScaleEnd < finish) {
                            ltf.Scale = 0;
                            state.doRecycle = true;
                        }
                    }
                    else {
                        state.doRecycle = true;
                        //Debug.Log($"Flagging missile for Recycling!!!");
                    }
                }

                // reset any flagged missiles
                {
                    if(state.doRecycle || resetIndices.IndexOf<int, int>(m.weaponIndex) != -1) {
                        state.Reset();
                        m.target = float3.zero;
                        m.currentHits = 0;
#if !UNITY_EDITOR
                        state.doRecycle = false;
#endif
                        m.distanceFromCenter = 0;
                        m.currentAngle = 0;
                        m.spinAngle = 0;
                        ltf.Rotation = quaternion.identity;
                        float newX = -50;
                        ltf.Position = new float3(newX, -2 * entity.Index, 0);
                        nuScale.Value = new float3(1, 1, 1);
                        //Debug.Log($"Recycling Missile {entity}!!!");
                        return;
                    }
                }

                if(state.isDying) return;

                // Missile is not dying past this point.
                state.timeAlive += dt;

                // movement
                {
                    var playerPos = float3.zero;
                    var moveDelta = frameStats.playerMoveDelta;
                    var bounds = new float2(ScreenBounds.X_Max + moveDelta.x, ScreenBounds.Y_Max + moveDelta.y);

                    switch(m.MoveType) {
                        case MissileMoveType.Forward: {
                            var desiredPosition = ltf.Position.xy + m.direction.xy * m.Speed * dt;

                            if((m.Flags & MissileFlags.Explodes) != 0) {
                                var desiredDir = desiredPosition - ltf.Position.xy;
                                var targetDir = m.target.xy - ltf.Position.xy;
                                if(math.lengthsq(desiredDir) >= math.lengthsq(targetDir)) {
                                    desiredPosition = m.target.xy;
                                }
                            }
                            ltf.Position.xy = desiredPosition.xy;
                        }
                        break;

                        case MissileMoveType.Orbit:
                        case MissileMoveType.Spiral: {
                            var spiral_radius = 0.1f;
                            var distanceCap = 3.5f + playerArea + m.Radius;
                            m.distanceFromCenter += dt * m.Speed * spiral_radius * 3f * (1 / (m.distanceFromCenter + 0.1f));
                            if(m.distanceFromCenter > distanceCap) m.distanceFromCenter = distanceCap;

                            // Update the angle
                            m.currentAngle += 35f * m.Speed * dt;

                            // Calculate the new position based on the angle and radius
                            var x = math.cos(math.radians(m.currentAngle)) * m.distanceFromCenter;
                            var y = math.sin(math.radians(m.currentAngle)) * m.distanceFromCenter;

                            switch(movement.OffsetType) {
                                case OffsetType.Default:
                                    m._spawnedPoint -= moveDelta;
                                    break;
                                case OffsetType.NoPlayerOffset:
                                    break;
                                case OffsetType.NoPlayerOffsetX:
                                    m._spawnedPoint -= moveDelta.y;

                                    break;
                                case OffsetType.NoPlayerOffsetY:
                                    m._spawnedPoint -= moveDelta.x;
                                    break;
                            }

                            ltf.Position = m._spawnedPoint + new float3(-x, y, 0);
                            break;
                        }

                        case MissileMoveType.Boomerang: {
                            ltf.Position.x += m.direction.x * (1920 / 683f) * dt * m._acceleration; // TODO: Magic Numbers.
                            ltf.Position.y += m.direction.y * (1080 / 456f) * dt * m._acceleration; // TODO: Magic Numbers.
                                                                                                    //UnityEngine.Debug.Log($"accel: {m._acceleration}, timeAlive:{m.timeAlive}, posY: {ltf.Position.y}");
                            m._acceleration -= 2f * dt;
                            if(m._acceleration < -2) m._acceleration = -2;
                        }
                        break;

                        case MissileMoveType.Arc: {
                            ltf.Position.xy += m.direction.xy * m.Speed * dt;
                            m.direction.y -= 10f / 2.81f * dt;
                        }
                        break;

                        case MissileMoveType.FollowsPlayer: {
                            if(math.any(m.target)) {
                                ltf.Position.xy = math.lerp(ltf.Position.xy, m.target.xy, dt);
                            }
                            if(random.NextFloat(0, 10) < 0.05f) {
                                var dir = random.NextFloat2Direction() * 1.5f;
                                m.target.xy = dir;
                            }
                        }
                        break;
                    }

                    m.target -= moveDelta;

                    // Wall Bounce
                    if((m.Flags & MissileFlags.BouncesOffWalls) != 0) {
                        if(ltf.Position.x >= bounds.x) {
                            m.direction.x = -1;
                        }
                        else if(ltf.Position.x <= -bounds.x) {
                            m.direction.x = 1;
                        }
                        if(ltf.Position.y >= bounds.y) {
                            m.direction.y = -1;
                        }
                        else if(ltf.Position.y <= -bounds.y) {
                            m.direction.y = 1;
                        }
                    }
                }

                // spinning
                {
                    var spinDir = frameStats.isPlayerFacingRight ? 1 : -1;
                    if((m.Flags & MissileFlags.Spins) != 0) {
                        var multiplier = m.spinSpeedMultiplier != 0 ? m.spinSpeedMultiplier : 1;
                        ltf.Rotation = quaternion.RotateZ(math.radians(m.spinAngle)).value;
                        m.spinAngle -= 360f * dt * multiplier * spinDir;
                        if(m.spinAngle < 0) {
                            m.spinAngle = 360;
                        }
                        else if(m.spinAngle > 360) {
                            m.spinAngle = 0;
                        }
                    }
                }

                // scaling
                {
                    if((m.Flags & MissileFlags.IgnoreScaling) == 0) {
                        var scaleCap = m.ScaleEnd * m.Radius; // @ScaleIssue: magic math
                        ltf.Scale += 5f * scaleCap * dt; // TODO: Hard coded numbers.
                        if(ltf.Scale >= scaleCap) {
                            ltf.Scale = scaleCap;
                        }
                    }

                    if(pulink.powerupComponent != nullEnt) {
                        // Scale the damage zone when the familiar is attacking
                        var powerup = AllPowerups[pulink.powerupComponent].PowerUp;
                        if(powerup.familiarTargetEntity != nullEnt) {
                            if((m.Flags & MissileFlags.IsFamiliarDamageZone) != 0) {
                                var attackingFlag = powerup.familiarTargetFlag;
                                var maxScale = 1.5f * 1.6f;
                                var minScale = 0.3f;
                                if(attackingFlag) {
                                    ltf.Scale += 10f * dt;
                                    if(ltf.Scale > maxScale) {
                                        ltf.Scale = maxScale;
                                    }
                                }
                                else {
                                    ltf.Scale -= 2f * dt;
                                    if(ltf.Scale < minScale) {
                                        ltf.Scale = minScale;
                                    }
                                }
                            }
                        }
                    }
                }

                // rotate towards direction
                {
                    if((m.Flags & MissileFlags.RotateDirection) != 0) {
                        //Debug.Log($"missileDir:{m.direction}");
                        if(math.any(m.direction)) {
                            ltf.Rotation = quaternion.LookRotation(new float3(0, 0, 1), m.direction);
                            var angle = math.degrees(quaternion.Euler(ltf.Rotation.value.xyz).value.z);
                            if(angle < 0) {
                                angle = 360 + angle;
                            }
                            bb.rotationAngle = angle;
                        }
                    }
                }
            }).Schedule(); // TODO: Can't ScheduleParallel() 
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class MissileOnDiedSystem : SystemBase {
    public NativeParallelMultiHashMap<System.Guid, SubMissileDeath> SubMissileTable;

    // NOTE: On Build, this system is called before the InitializeMissiles[...]System. So we have to use OnStartRunning instead
    protected override void OnStartRunning() {
        base.OnStartRunning();

        if(!SubMissileTable.IsCreated)
            SubMissileTable = new(100, Allocator.Persistent);
        else
            SubMissileTable.Clear();

        #region Set up SubMissiles on Death
        // Hook up Submissiles to their Missile "parent".
        var methods = typeof(Missiles).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var id_index = -1;

        for(var i = 0; i < methods.Length; i++) {
            var method = methods[i];
            if(method.ReturnType != typeof(MissileArchetype))
                continue;

            // we found a missile, increment the id index
            id_index++;

            // check for submissiles
            for(var j = i + 1; j < methods.Length; j++) {
                var submissileMethod = methods[j];
                if(submissileMethod == null || submissileMethod.ReturnType != typeof(SubMissileDeath))
                    break;

                // we found a submissile
                var m = (SubMissileDeath)submissileMethod.Invoke(null, null);
                // NOTE: ids and missiles are aligned
                var parentMissileID = Missiles.ids[id_index].Guid;
                //Debug.Log($"found submissile method: {submissileMethod.Name}, parentMissileID: {parentMissileID}");
                if(parentMissileID == System.Guid.Empty) {
                    Debug.LogError("Got an empty parent missile for the SubMissile Table!");
                    break;
                }
                SubMissileTable.Add(parentMissileID, m);
                //Debug.Log($"Adding {submissileMethod.Name} to SubMissile Table! id_index:{id_index}");
            }
        }
        #endregion
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if(SubMissileTable.IsCreated) SubMissileTable.Dispose();
    }

    protected override void OnUpdate() {
        var time = UnityEngine.Time.time;
        var bounds = ScreenBounds.Outer_Max_XY;
        var frame = UnityEngine.Time.frameCount;

        Entities.ForEach((Entity entity, ref State state, in Missile missile, in LocalTransform ltf, in SpriteFrameData sf_data, in ID id, in PowerUpLink pulink) => {
            if(!state.isActive) return;
            if(state.isDying) return;
            var died = false;

            if((missile.Flags & MissileFlags.IsPiercing) != 0 && missile.Piercing == 0) {
                SetDied("Piercing is 0", ref died);
            }

            if(state.timeAlive >= missile.Duration && (missile.Flags & MissileFlags.HasTimedLife) != 0) {
                SetDied($"timeAlive:{state.timeAlive} >= Duration:{missile.Duration}", ref died);
            }

            if((missile.Flags & MissileFlags.Explodes) != 0 && math.distancesq(ltf.Position.xy, missile.target.xy) < 0.15f) {
                SetDied("Exploding missile reached its target", ref died);
            }

            if(sf_data.loopCount >= 1 && (missile.Flags & MissileFlags.KillAfterAnimation) != 0) {
                SetDied("Kill After Animation", ref died);
            }

            if((missile.Flags & MissileFlags.NoDespawn) == 0) {
                if(ltf.Position.x >= bounds.x + 4 || ltf.Position.x <= -bounds.x - 4 || ltf.Position.y >= bounds.y + 3 || ltf.Position.y <= -bounds.y - 3) {
                    SetDied("Out of bounds", ref died);
                }
            }

            if(died) {
                state.isDying = true;
                state.timeOfDeath = time;
                state.frameOfDeath = frame;
            }
        }).Schedule();
    }

    static void SetDied(string reason, ref bool died) {
        died = true;
        //Debug.Log($"MissileDied::{reason}");
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MissileRecycleSystem : SystemBase {

    protected override void OnUpdate() {
        Entities
            .WithAll<Missile>()
            .WithName("SpawnMissileSetSpriteAndParticleSystem")
            .ForEach((Entity e, SpriteRenderer sr, ParticleSystem pslink, ref State state, ref SpriteFrameData sf_data, in ID id, in Gfx gfx) => {
                if(state.needsSpriteAndParticleSystem) {
                    state.isActive = true;
                    state.needsSpriteAndParticleSystem = false;
                    (sr.sprite, sf_data.frameCount) = SpriteDB.Instance.Get(id.Guid);
                    sf_data.frameTimer = 0;
                    sf_data.currentFrame = 0;
                    sf_data.loopCount = 0;

                    ParticleSystemHelper.SetParticleSystem(id.Guid, pslink);
                    #if UNITY_EDITOR
                    EntityManager.SetName(e, $"Missile {e} - Gfx {gfx.spriteName}");
                    #endif
                }
                #if UNITY_EDITOR
                else if(state.doRecycle) {
                    state.doRecycle = false;
                    EntityManager.SetName(e, $"Missile {e}");
                }
                #endif
            }).WithoutBurst().Run();
    }
}