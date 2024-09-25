using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics;
using System;

[UpdateInGroup(typeof(Init1))]
public partial class MissileSpawnSystem : SystemBase {
    [Serializable]
    struct SubmissileData {
        public float3 position;
        public ID missileID;
        public Entity powerup;
    }

    public NativeArray<bool> PlayerHitFlag;
    static AudioClip normalWeapon;
    static AudioClip magicWeapon;
    static AudioSource AudioSource;

    static readonly float[] __angles_5_to_45 = {
        0,
        5,
        - 5,
        10,
        - 10,
        15,
        - 15,
        20,
        - 20,
        25,
        - 25,
        30,
        - 30,
        35,
        - 35,
        40,
        - 40,
        45,
        - 45
    };

    static readonly float[] __angles_10_to_40 = {
           0,
          10,
         -10,
          20,
         -20,
          30,
         -30,
          40,
         -40
    };

    static NativeArray<float> _angles_5_to_45;
    static NativeArray<float> _angles_10_to_40;

    protected override void OnCreate() {
        _angles_5_to_45 = new NativeArray<float>(__angles_5_to_45, Allocator.Persistent);
        _angles_10_to_40 = new NativeArray<float>(__angles_10_to_40, Allocator.Persistent);

        base.OnCreate();

        if(!PlayerHitFlag.IsCreated)
            PlayerHitFlag = new NativeArray<bool>(1, Allocator.Persistent);

        magicWeapon = Resources.Load<AudioClip>("Audio/VS_ProjectileMagic_v04-03");
        Debug.Assert(magicWeapon, "Assertion Failed! Couldn't find AudioClip.");
        normalWeapon = Resources.Load<AudioClip>("Audio/VS_Projectile_v06-02");
        Debug.Assert(normalWeapon, "Assertion Failed! Couldn't find AudioClip.");
        AudioSource = GameObject.Find("Audio Source Launch").GetComponent<AudioSource>();
        Debug.Assert(AudioSource);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if(PlayerHitFlag.IsCreated)
            PlayerHitFlag.Dispose();

        if(_angles_5_to_45.IsCreated)
            _angles_5_to_45.Dispose();
        
        if(_angles_10_to_40.IsCreated)
            _angles_10_to_40.Dispose();
    }

    protected override void OnUpdate() {

        var playerPos = float3.zero;
        var distance    = new NativeArray<float>(1, Allocator.TempJob);
        distance[0]     = 9999f;
        var nearest     = new NativeArray<float3>(1, Allocator.TempJob);
        nearest[0]      = new float3(-1);

        var hits = new NativeList<DistanceHit>(100, Allocator.TempJob);
        var cf = new CollisionFilter {
            BelongsTo    = 1u << 1 | 1u << 2, // Enemies and Swarmers
            CollidesWith = 1u << 1 | 1u << 2  // Enemies and Swarmers
        };

        var singleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        Dependency = Job
        .WithName("SetClosestEnemyAndSetRandomEnemy")
        .WithDisposeOnCompletion(distance)
        .WithCode(() => {
            if(singleton.OverlapBox(float3.zero, quaternion.identity, new float3(ScreenBounds.X_Max, ScreenBounds.Y_Max, 0.5f), ref hits, cf)) {
                foreach(var hit in hits) {
                    var dist = math.distancesq(playerPos, hit.Position);
                    if(dist < distance[0]) {
                        nearest[0] = hit.Position;
                        distance[0] = dist;
                    }
                }
            }
        }).Schedule(Dependency);
        
        var MousePosition           = (float3)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var random                  = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 1_000_000));
        var chara                   = SystemAPI.GetSingleton<CharacterComponent>().character;
        var pCdr                    = chara.CharacterStats.Get(Stats.CooldownReductionID).value;
        var area                    = chara.CharacterStats.Get(Stats.AreaID).value + 1; // TODO: does the player stat include all bonuses to Area?
        var pAmount                 = chara.CharacterStats.Get(Stats.AmountID).value;
        var frameStats              = SystemAPI.GetSingleton<FrameStatsSingleton>();
        var isPLayerFacingRight     = frameStats.isPlayerFacingRight;
        var missileTable            = Missiles.MissileTable;
        var subMissileTable         = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MissileOnDiedSystem>().SubMissileTable;
        var playerHitFlag           = PlayerHitFlag[0];
        var InactiveMissileEntities = new NativeList<Entity>(Allocator.TempJob);
        var angles_5_45             = _angles_5_to_45;
        var angles_10_40            = _angles_10_to_40;
        var dt                      = UnityEngine.Time.deltaTime;

        Dependency = Entities
            .WithName("UpdateWeaponAndPowerup")
            .ForEach((ref DynamicBuffer<WeaponComponent> weaponBuffer, in PowerUpComponent puc) => {
                for(var i = 0; i < weaponBuffer.Length; i++) {
                    var powerup = puc.PowerUp;
                    if(powerup.level == 0) continue;
                    var weapon = weaponBuffer[i].Weapon;
                    PowerUp.UpdateTimers(powerup, ref weapon, pCdr, dt);
                    var wc = weaponBuffer[i];
                    wc.Weapon = weapon;
                    weaponBuffer[i] = wc;
                }
            }).Schedule(Dependency);

        Dependency = Entities
            .WithName("GetInactiveMissileEntities")
            .ForEach((MissileAspect m) => {
                if(m.State.ValueRO.isActive || m.State.ValueRO.needsSpriteAndParticleSystem) return;
                InactiveMissileEntities.Add(m.Self);
            }).Schedule(Dependency);

        Dependency = Entities
            .WithName("SpawnMissiles")
            .WithReadOnly(missileTable)
            .WithReadOnly(hits)
            .WithReadOnly(nearest)
            .WithDisposeOnCompletion(hits)
            .WithDisposeOnCompletion(nearest)
            .WithReadOnly(InactiveMissileEntities)
            .ForEach((Entity e, ref PowerUpComponent puc, ref DynamicBuffer<WeaponComponent> weaponBuffer, ref DynamicBuffer<WeaponMissileEntity> missileBuffer) => {
                for(var i = 0; i < weaponBuffer.Length; i ++) {
                    var weapon = weaponBuffer[i].Weapon;
                    var missileArch = missileTable[weapon.missileData.missileID.Guid];
                    var powerup = puc.PowerUp;
                    Debug.Assert(!powerup.name.IsEmpty && powerup.maxLevel != 0, "SpawnMissile powerup was null!");

                    if(powerup.level == 0) {
                        if(missileBuffer.Length > 0) {
                            // found some missile remnants.
                            missileBuffer.Clear();
                        }
                        // TODO: Maybe we can do better than just continuing?
                        continue;
                    }

                    var amount = GetAmount(pAmount, powerup, weapon);
                    ushort batchSize = 1;
                    switch(weapon.PowerUpShootType) {
                        case ShootType.Interval:
                            if(powerup.baseStats.Interval == 0) { // For Bone.
                                batchSize = amount;
                            }
                            else {
                                var t = dt / weapon.baseStats.Interval;
                                batchSize = (ushort)math.max(1, t);
                                if(batchSize == 0) batchSize = 1;
                                //Debug.Log($"batchSize: {batchSize}, t: {t}, dt: {dt}");
                            }
                            break;
                        case ShootType.BatchedMissiles:
                            batchSize = amount;
                            break;
                    }

                    if((missileArch.missile.Flags & MissileFlags.IsFamiliar) != 0) { // Force batchSize of 1 for Familiar missiles.
                        batchSize = 1;
                    }

                    var randomPos = new float3(-1);
                    if(hits.Length > 0) {
                        var randomIndex = random.NextInt(0, hits.Length);
                        randomPos = hits[randomIndex].Position;
                        randomPos.z = 0;
                    }
                    
                    var familiarPos = new float3(-1);
                    var spawnType = weapon.missileData.SpawnPositionType;
                    var spawnDirection = weapon.missileData.SpawnDirection;
                    var variationType = weapon.missileData.SpawnVariationType;

                    #region Check if the Missile can be spawned.

                    if(weapon.delay > 0) {
                        //Debug.Log($"Couldn't spawn because of delay: {weapon.delay}");
                        continue;
                    }

                    switch(spawnType) {
                        case SpawnPositionType.AtRandomEnemy:
                            if(randomPos.z == -1) {
                                Debug.LogWarning("Couldn't find a random enemy, returning!");
                                continue;
                            }
                            break;
                    }

                    switch(spawnDirection) {
                        case Directions.NearestEnemy_Then_Clockwise:
                        case Directions.NearestEnemyDirection:
                            if(nearest[0].z == -1) {
                                Debug.LogWarning("Couldn't find a random enemy, returning!");
                                continue;
                            }
                            break;

                        case Directions.RandomEnemyDirection:
                            if(randomPos.z == -1) {
                                Debug.LogWarning("Couldn't find a random enemy, returning!");
                                continue;
                            }
                            break;
                    }

                    switch(powerup.PowerUpType) {
                        case PowerUpType.ShootsMissile: {

                            if(weapon._cooldownTimer > 0) {
                                //Debug.Log($"couldn't spawn because of cooldown: {weapon._cooldownTimer}");
                                continue;
                            }

                            switch(weapon.PowerUpShootType) {
                                case ShootType.None:
                                    continue;

                                case ShootType.FiresOnlyOnceEver: {
                                    if(weapon._firedSoFar > 0) {
                                        continue;
                                    }
                                }
                                break;

                                case ShootType.ActivatesOnPlayerHit: {
                                    if(playerHitFlag) {
                                        continue;
                                    }
                                }
                                break;

                                case ShootType.BatchedMissiles: {
                                    if(missileBuffer.Length != 0) {
                                        //Debug.Log($"couldn't spawn missile, buffer.Length: {buffer.Length}, firedSoFar: {weapon._firedSoFar}");
                                        continue;
                                    }

                                    if(weapon._firedSoFar >= batchSize || weapon._firedSoFar > 0) {
                                        //Debug.Log($"couldn't spawn missile, batchsize: {batchSize}, firedSoFar: {weapon._firedSoFar}");
                                        continue;
                                    }
                                }
                                break;

                                case ShootType.Interval: {
                                    if(weapon._firedSoFar > 0) {
                                        if(weapon._intervalTick != 0) {
                                            continue;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        break;

                        case PowerUpType.ChargedBuff: {
                            if(powerup.baseStats.Amount == 0) {
                                Debug.LogWarning($"charged buff powerup {powerup.name} had Amount of 0!");
                                continue;
                            }

                            if(powerup.baseStats.Charges > 0) {
                                switch(weapon.PowerUpShootType) {
                                    case ShootType.ActivatesOnPlayerHit: {
                                        if(!playerHitFlag) {
                                            //Debug.Log($"playerHitFlag is false");
                                            continue;
                                        }
                                    }
                                    break;
                                }
                            }

                            if(powerup.baseStats.Charges == 0) {
                                //Debug.Log($"charges is 0");
                                continue;
                            }
                        }
                        break;
                    }

                    if(powerup.familiarEntity != Entity.Null) {
                        familiarPos = SystemAPI.GetAspect<MissileAspect>(powerup.familiarEntity).Transform.ValueRO.Position;
                        if((missileArch.missile.Flags & MissileFlags.IsFamiliar) != 0) {
                            Debug.Log($"Couldn't spawn because missile is a familiar and a familiar already spawned.");
                            continue;
                        }
                    }
                    else if(spawnType == SpawnPositionType.AtFamiliar) {
                        Debug.Log($"Couldn't spawn because we don't have a familiar to spawn to.");
                        continue;
                    }

                    #endregion

                    #region Get a Spawn Position for the Missile.
                    var spawnPosition = new float3(0, 0f, -0.5f);

                    switch(spawnType) {
                        case SpawnPositionType.AtRandomEnemy:
                            spawnPosition = randomPos;
                            break;

                        case SpawnPositionType.AtFamiliar: {
                            spawnPosition = familiarPos;
                        }
                        break;
                    }

                    switch(variationType) {
                        case SpawnVariationType.FlipsUpward:
                            spawnPosition.y += 0.16f * 2.81f * weapon._firedSoFar;
                            break;

                        case SpawnVariationType.RandomSpread: {
                            var a = -0.35f;
                            var b = 0.35f;
                            spawnPosition.x += random.NextFloat(a, b) * weapon._firedSoFar;
                            spawnPosition.y += random.NextFloat(a, b) * weapon._firedSoFar;
                        }
                        break;

                        case SpawnVariationType.RandomSpreadWithArea_Knife: {
                            var r = random.NextFloat(-1f, 1f);
                            var x = r * (0 == weapon._firedSoFar ? 0 : 0.7f);
                            r = random.NextFloat(-1f, 1f);
                            var y = r * (0 == weapon._firedSoFar ? 0 : 0.7f);
                            spawnPosition.x += x * area;
                            spawnPosition.y += y * area;
                        }
                        break;

                        case SpawnVariationType.RandomSpreadWithArea2_Cross: {
                            spawnPosition.x += random.NextFloat(-1f, 1f) * weapon._firedSoFar * 0.20f * 2.81f * area;
                            spawnPosition.y += random.NextFloat(-1f, 1f) * weapon._firedSoFar * 0.20f * 2.81f * area;
                        }
                        break;

                        case SpawnVariationType.FamiliarRandomSpread: {
                            var range = missileArch.missile.Radius * 1.25f;
                            var r = random.NextFloat(-range, range);
                            var x = r * (0 == weapon._firedSoFar ? 0 : 0.7f);
                            r = random.NextFloat(-range, range);
                            var y = r * (0 == weapon._firedSoFar ? 0 : 0.7f);
                            spawnPosition.x += x;
                            spawnPosition.y += y;
                        }
                        break;
                    }

                    // This seems hacky?
                    if(weapon.spawnOffset.z != -1) {
                        spawnPosition += weapon.spawnOffset;
                    }
                    else {
                        spawnPosition += missileArch.missile.spawnOffset;
                    }

                    //
                    // TODO: Combine MissileFlags.Flips and FlipsUpwards into one section instead of having them separated...
                    //
                    var flips = (missileArch.missile.Flags & MissileFlags.Flips) != 0 || (weapon.missileData.additionalMissileFlags & MissileFlags.Flips) != 0;
                    if(flips && !isPLayerFacingRight) {
                        spawnPosition.x *= -1;
                    }
                    #endregion

                    if(spawnDirection == Directions.TowardsFamiliarDamageZone && powerup.familiarTargetEntity != Entity.Null) { // @Familiar
                        powerup.familiarTargetFlag = true;
                    }

                    #region SpawnInactiveMissile
                    
                    var missileEntity                               = Entity.Null;
                    var foundAllRequestedMissiles                   = new NativeReference<bool>(false, Allocator.Temp);
                    var foundEnemy                                  = new NativeReference<bool>(false, Allocator.Temp);
                    var foundCount                                  = new NativeReference<ushort>(0, Allocator.Temp);
                    var firedSoFar                                  = new NativeReference<ushort>(weapon._firedSoFar, Allocator.Temp);
                    var firedTotal                                  = new NativeReference<ushort>(weapon._firedTotal, Allocator.Temp);
                    var missileEntities                             = missileBuffer;
                    var isFamiliar                                  = (missileArch.missile.Flags & MissileFlags.IsFamiliar) != 0 && powerup.familiarEntity == Entity.Null;
                    var needsTargetFamiliar                         = (missileArch.missile.Flags & MissileFlags.IsFamiliarDamageZone) != 0 && powerup.familiarEntity != Entity.Null && powerup.familiarTargetEntity == Entity.Null;
                    var damageZonePosition                          = powerup.familiarTargetEntity != Entity.Null ? SystemAPI.GetAspect<MissileAspect>(powerup.familiarTargetEntity).Transform.ValueRO.Position : new float3(-1);

                    for(var j = 0; j < InactiveMissileEntities.Length && !foundAllRequestedMissiles.Value; j++) {
                        var missile                                 = SystemAPI.GetAspect<MissileAspect>(InactiveMissileEntities[j]);
                        if(missile.State.ValueRO.isActive) {
                            //Debug.Log($"Failed: missile is Active!");
                            continue;
                        }
                        if(missile.State.ValueRO.needsSpriteAndParticleSystem) {
                            //Debug.Log($"Failed: missile needs sprite and P.S.");
                            continue;
                        }
                        if(missile.State.ValueRO.isDying) {
                            //Debug.Log($"Failed: missile is Dying");
                            continue;
                        }
                        if(foundAllRequestedMissiles.Value) {
                            //Debug.Log($"Failed: missiles are found!");
                            continue;
                        }

                        var missileID                               = weapon.missileData.missileID;
                        var angleSpacing                            = batchSize > 1 ? 360f / batchSize : 180f;
                        var previousPlayerMoveDelta                 = frameStats.previousPlayerMoveDelta;
                        var weaponShootType                         = weapon.PowerUpShootType;
                        var Target                                  = weapon.targetDirection;
                        var additionalMissileFlags                  = weapon.missileData.additionalMissileFlags;
                        var removeMissileFlags                      = weapon.missileData.removeMissileFlags;
                        
                        missile.Missile.ValueRW                     = missileArch.missile;

                        Debug.Assert(!powerup.name.IsEmpty && powerup.maxLevel != 0, "SpawnMissile powerup was null!");
                        Debug.Assert(missile.Missile.ValueRO.Radius != 0, $"Radius: {missile.Missile.ValueRO.Radius}");
                        
                        missile.Missile.ValueRW.SpawnPositionType   = spawnType;    // Just for debugging in the inspector, since a weapon may override this.
                        switch(spawnType) {
                            case SpawnPositionType.AtRandomEnemy:
                                if(randomPos.z == -1) {
                                    Debug.LogWarning("Couldn't find a random enemy, returning!");
                                    continue;
                                }
                                break;
                        }
                        
                        missile.Missile.ValueRW.SpawnDirection      = spawnDirection;  // Just for debugging in the inspector, since a weapon may override this.
                        switch(spawnDirection) {
                            case Directions.NearestEnemyDirection:
                                if(nearest[0].z == -1) {
                                    Debug.LogWarning("Couldn't find nearest enemy, returning!");
                                    continue;
                                }
                                break;
                            case Directions.TowardsFamiliarDamageZone: {
                                if(powerup.familiarTargetEntity == Entity.Null) {
                                    //Debug.LogError($"couldn't find a familiar damage zone entity!");
                                    continue;
                                }
                            }
                            break;
                        }
                        foundEnemy.Value = true;

                        SetMissileData(missile, powerup, missileID, missileArch);
                        
                        missileEntity                               = missile.Self;
                        missile.pulink.ValueRW.powerupComponent     = e;

                        if(additionalMissileFlags != MissileFlags.None) {
                            missile.Missile.ValueRW.Flags |= additionalMissileFlags;
                        }
                        if(removeMissileFlags != MissileFlags.None) {
                            missile.Missile.ValueRW.Flags &= ~removeMissileFlags;
                        }

                        // TODO: Is this the best place to do this?
                        missile.Transform.ValueRW.Rotation = quaternion.identity;

                        switch(weaponShootType) {
                            case ShootType.BatchedMissiles:
                                missile.Missile.ValueRW.firedIndex = foundCount.Value;
                                break;
                            case ShootType.Interval:
                                missile.Missile.ValueRW.firedIndex = firedSoFar.Value;
                                break;
                        }

                        if(spawnDirection != Directions.Clockwise) {
                            missile.BoundingBox.ValueRW.rotationAngle = 0;
                        }

                        var MissileDirection = new float3(-1);
                        switch(spawnDirection) {
                            case Directions.RandomEnemyDirection: {
                                Target = randomPos;
                                MissileDirection = math.normalizesafe(Target - spawnPosition);
                            }
                            break;

                            case Directions.RandomDirection: {
                                var direction = random.NextFloat2Direction();
                                MissileDirection = new float3(direction.x, direction.y, 0);
                            }
                            break;

                            case Directions.NearestEnemyDirection: {
                                Target = nearest[0];
                                var direction = math.normalizesafe(Target.xy - spawnPosition.xy);
                                MissileDirection = new float3(direction.x, direction.y, 0);
                            }
                            break;

                            case Directions.NearestEnemy_Then_Clockwise: {
                                var dir = float3.zero;
                                if(amount < 4) {
                                    if(firedSoFar.Value == 0) {
                                        dir.xy = math.normalizesafe(nearest[0].xy - spawnPosition.xy);
                                        MissileDirection = new float3(dir.x, dir.y, 0);
                                        Target = nearest[0];
                                    }
                                    else {
                                        SetTargetAndDirAnglePosition(ref MissileDirection, ref Target, firedTotal.Value, spawnPosition);
                                    }
                                }
                                else {
                                    SetTargetAndDirAnglePosition(ref MissileDirection, ref Target, firedTotal.Value, spawnPosition);
                                }
                            }
                            break;

                            case Directions.Mouse_Pos: {
                                MousePosition.z = spawnPosition.z;
                                Target = MousePosition;
                                MissileDirection = math.normalizesafe(MousePosition - spawnPosition);
                            }
                            break;

                            case Directions.Clockwise: {
                                var angleIncrement = 360f / 12f;
                                var a = (-firedTotal.Value + 3) * angleIncrement % 360; // TODO: Magic Number 3.
                                if(a < 0) a += 360;
                                var a_radians = math.radians(a);
                                missile.Transform.ValueRW.Rotation = quaternion.RotateZ(a_radians);
                                missile.BoundingBox.ValueRW.rotationAngle = a;
                                MissileDirection = new float3(math.sin(a_radians), math.cos(a_radians), spawnPosition.z);
                                spawnPosition += MissileDirection.yxz * 1.8f; // TODO: Magic Number (should be BoundingBox extents.y).
                            }
                            break;

                            case Directions.TowardsFamiliarDamageZone: {
                                if(powerup.familiarTargetEntity != Entity.Null) {
                                    var originalPos = damageZonePosition;
                                    var pos = originalPos;
                                    var range = 1;
                                    var rx = random.NextFloat(-range, range);
                                    var ry = random.NextFloat(-range, range);
                                    pos.x += rx;
                                    pos.y += ry;
                                    var dir = math.normalizesafe(pos - spawnPosition);
                                    dir.z = 0;
                                    if(!math.any(dir)) {
                                        dir.y = 1;
                                    }
                                    Target = pos;
                                    MissileDirection = dir;
                                }
                            }
                            break;

                            case Directions.PlayersLastMovement: {
                                MissileDirection = math.normalizesafe(previousPlayerMoveDelta);
                            }
                            break;

                            case Directions.Angles_5_to_45: {
                                if(!math.any(Target) || Target.z == -1) { // TODO: Maybe TargetDirection should be set earlier?
                                    Target = randomPos;
                                }
                                SetDirectionFromAngles(ref MissileDirection, Target, firedSoFar.Value, spawnPosition, angles_5_45);
                            }
                            break;

                            case Directions.Angles_10_to_40: {
                                if(!math.any(Target) || Target.z == -1) { // TODO: Maybe TargetDirection should be set earlier?
                                    Target = randomPos;
                                }
                                SetDirectionFromAngles(ref MissileDirection, Target, firedSoFar.Value, spawnPosition, angles_10_40);
                            }
                            break;

                            case Directions.Sideways: {
                                MissileDirection = new float3(1, 0, 0);
                                missile.BoundingBox.ValueRW.rotationAngle = 270;
                            }
                            break;
                        }

                        switch(missile.Missile.ValueRW.MoveType) {
                            case MissileMoveType.Orbit:
                                missile.Missile.ValueRW.currentAngle = angleSpacing * missile.Missile.ValueRW.firedIndex;
                                missile.Missile.ValueRW.spinSpeedMultiplier = 1 * amount * 0.3f;
                                break;

                            case MissileMoveType.Boomerang:
                                // NOTE: If the missile.direction.z is not set to 0, the missiles spawnOffset.z might introduce
                                // undesired movement along the Z-Axis.
                                missile.Missile.ValueRW.direction.z = 0;
                                missile.Missile.ValueRW._acceleration = 1.5f + 0.1f * missile.Missile.ValueRW.firedIndex;
                                break;

                            case MissileMoveType.Arc:
                                var flip = isPLayerFacingRight ? -1 : 1;
                                var _angle = 45 * flip / amount * firedSoFar.Value + 90;
                                MissileDirection = AngleToDirection(_angle) * missile.Missile.ValueRW.Acceleration;
                            break;

                        }

                        missile.Missile.ValueRW.target      = Target;
                        missile.Missile.ValueRW.direction   = MissileDirection;

                        //
                        // TODO: Combine MissileFlags.Flips and FlipsUpwards into one section instead of having them separated...
                        //
                        if((missile.Missile.ValueRW.Flags & MissileFlags.Flips) != 0) {
                            if(variationType == SpawnVariationType.FlipsUpward) {
                                var flipX = !isPLayerFacingRight ? firedSoFar.Value % 2 != 1 : firedSoFar.Value % 2 == 1;
                                var flipY = firedSoFar.Value % 2 == 1;

                                missile.nonUniformScale.ValueRW.Value.xy *= new float2(flipX ? -1 : 1, flipY ? -1 : 1);
                                if(flipY) {
                                    spawnPosition.x *= -1;
                                }
                                
                            }
                            else {
                                missile.nonUniformScale.ValueRW.Value.x *= !isPLayerFacingRight ? -1 : 1;
                            }
                        }
                        
                        missile.Transform.ValueRW.Position = spawnPosition;
                        missile.Missile.ValueRW._spawnedPoint = spawnPosition; // Prevent the spiral missiles from "following" the player's movement.


                        missileEntities.Add(new WeaponMissileEntity { MissileEntity = missileEntity });
                        weapon.targetDirection = Target;
                        missile.State.ValueRW.needsSpriteAndParticleSystem = true;

                        foundCount.Value += 1;
                        foundAllRequestedMissiles.Value = foundCount.Value >= batchSize;

                        if(isFamiliar && missileEntity != Entity.Null && powerup.familiarEntity == Entity.Null) {
                            //Debug.Log($"setting familiar to {missileEntity.Index}, isFamiliar: {isFamiliar}, missileEnt: {missileEntity.Index}, powerupFamiliar: {powerup.familiarEntity.Index}");
                            powerup.familiarEntity = missileEntity;
                            powerup.familiarSpawnedFlag = true;
                        }
                        else if(needsTargetFamiliar && missileEntity != Entity.Null) {
                            powerup.familiarTargetEntity = missileEntity;
                            //Debug.Log($"setting familiar damage zone to {missileEntity.Index}");
                        }
                    }

                    missileBuffer = missileEntities;

                    if(foundEnemy.Value && foundAllRequestedMissiles.Value) { // Successfully found an enemy and all requested missiles
                        weapon._firedSoFar += batchSize;
                        weapon._firedTotal += batchSize;
                        powerup.baseStats.Charges -= 1;
                        if(powerup.baseStats.Charges < 0) {
                            powerup.baseStats.Charges = 0;
                        }
                        powerup._durationTimer = missileArch.missile.Duration;
                        
                        // TODO: Can't play audio in Bursted job.
                        //PlayMissileLaunchForHitEffect(sourceMissile.HitEffect); 
                    }

                    if(!foundEnemy.Value || !foundAllRequestedMissiles.Value) {
                        //Debug.LogWarning($"Couldn't find a missile, or perhaps an enemy. foundEnemy: {foundEnemy.Value}, foundMissile: {foundAllRequestedMissiles.Value}, findCount: {foundCount.Value}, batchSize: {batchSize}");
                    }

                    #endregion
                    
                    var wc = weaponBuffer[i];
                    wc.Weapon = weapon;
                    weaponBuffer[i] = wc;
                    puc.PowerUp = powerup;
                }
            }).Schedule(Dependency);

        var submissiles = new NativeList<SubmissileData>(Allocator.TempJob);
        var frameCount  = UnityEngine.Time.frameCount;

        Dependency = Entities
            .WithName("SetSubMissiles")
            .ForEach((MissileAspect missile) => {
                if(missile.State.ValueRO.isDying && missile.State.ValueRO.frameOfDeath == frameCount - 1) { // TODO HACK: The Missile Death System runs after this System so there's a one frame difference between the Missiles death and the checking if its dead.
                    submissiles.Add(new SubmissileData {
                        missileID = missile.Id.ValueRO,
                        position  = missile.Transform.ValueRO.Position,
                        powerup   = missile.pulink.ValueRO.powerupComponent,
                    });
                    //Debug.Log($"time of death, frame - 1: {frame - 1}, frameOfDeath: {missile.State.ValueRO.frameOfDeath}");
                }
            }).Schedule(Dependency);

        Dependency = Job
            .WithName("SpawnSubMissiles")
            .WithReadOnly(subMissileTable)
            .WithReadOnly(missileTable)
            .WithReadOnly(InactiveMissileEntities)
            .WithDisposeOnCompletion(InactiveMissileEntities)
            .WithReadOnly(submissiles)
            .WithDisposeOnCompletion(submissiles)
            .WithCode(() => {
                var AllPowerups = GetComponentLookup<PowerUpComponent>(true);
                foreach(var sm in submissiles) {
                    var values = subMissileTable.GetValuesForKey(sm.missileID.Guid);
                    foreach(var smd in values) {
                        var missileID       = smd.missileToSpawnID;
                        var missileArch     = missileTable[smd.missileToSpawnID.Guid];
                        var powerup         = AllPowerups[sm.powerup].PowerUp;
                        Debug.Assert(!powerup.name.IsEmpty && powerup.maxLevel != 0, "SpawnMissile powerup was null!");

                        var foundCount                          = new NativeReference<ushort>(0, Allocator.Temp);
                        var foundAllRequestedMissiles           = new NativeReference<bool>(false, Allocator.Temp);

                        for(var i = 0; i < InactiveMissileEntities.Length && !foundAllRequestedMissiles.Value; i++) {
                            var missile = SystemAPI.GetAspect<MissileAspect>(InactiveMissileEntities[i]);
                            if(missile.State.ValueRO.isActive) continue;
                            if(missile.State.ValueRO.isDying) continue;
                            if(missile.State.ValueRO.needsSpriteAndParticleSystem) continue;
                            
                            missile.Missile.ValueRW             = missileArch.missile;
                            missile.Transform.ValueRW.Position  = sm.position;
                            missile.Transform.ValueRW.Rotation  = quaternion.identity;

                            Debug.Assert(missile.Missile.ValueRW.Radius != 0);
                            SetMissileData(missile, powerup, missileID, missileArch);
                            missile.State.ValueRW.needsSpriteAndParticleSystem = true;

                            foundCount.Value += 1;
                            foundAllRequestedMissiles.Value = foundCount.Value >= 1;
                        }

                        foundCount.Value = 0;
                        foundAllRequestedMissiles.Value = false;
                    }
                }
            }).Schedule(Dependency);

        Dependency = Entities
           .WithName("UpdateWeaponAndPowerupAfterMissileSpawn")
           .ForEach((Entity e, ref PowerUpComponent puc, ref DynamicBuffer<WeaponComponent> weaponBuffer, ref DynamicBuffer<WeaponMissileEntity> missileBuffer) => {
               for(var i = 0; i < weaponBuffer.Length; i++) {
                   var weapon = weaponBuffer[i].Weapon;
                   var powerup = puc.PowerUp;

                   if(powerup.level == 0) continue;
                   
                   if(powerup.familiarSpawnedFlag && weapon.missileData.SpawnPositionType == SpawnPositionType.AtFamiliar) {
                       weapon._intervalTick = 0;
                       weapon._cooldownTimer = 0;
                       weapon._firedSoFar = 0;
                       powerup.familiarSpawnedFlag = false;
                   }

                   var amount = GetAmount(pAmount, powerup, weapon);
                   
                   // Update Weapons
                   switch(powerup.PowerUpType) {
                       case PowerUpType.ShootsMissile: {

                           switch(weapon.PowerUpShootType) {
                               case ShootType.Interval: {
                                   if(weapon._firedSoFar > 0) {
                                       weapon._intervalTick += dt;
                                   }
                               }
                               break;
                           }
                       }
                       break;

                       case PowerUpType.ChargedBuff: {
                           if(powerup.baseStats.Charges != powerup.baseStats.Amount) {
                               weapon._cooldownTimer -= dt;
                           }
                       }
                       break;
                   }
                   
                   // Try to Reset Weapons
                   switch(powerup.PowerUpType) {
                       case PowerUpType.ChargedBuff:
                           if(weapon._cooldownTimer <= 0) {
                               ResetWeapon(ref powerup, ref weapon, pCdr);
                           }
                           break;
                       case PowerUpType.Bonus:
                           break;
                   }

                   switch(weapon.PowerUpShootType) {
                       case ShootType.BatchedMissiles:
                           if(missileBuffer.Length == 0 && weapon._firedSoFar > 0) {
                               ResetWeapon(ref powerup, ref weapon, pCdr);
                               //Debug.Log($"resetting batched missile weapon, {powerup.name}, buffer.Length: {buffer.Length}, firedSoFar: {weapon._firedSoFar}");
                           }
                           else {
                               //Debug.Log($"couldn't reset batched missile weapon. buffer.Length: {buffer.Length}, firedSoFar: {weapon._firedSoFar}!!");
                           }

                           break;
                       case ShootType.Interval:
                           var interval = !GrowthStats.IsDefault(weapon.baseStats) ? weapon.baseStats.Interval : powerup.baseStats.Interval;
                           //Debug.Log($"interval : {interval}, amount: {amount}");
                           //var interval = weapon.baseStats != null ? weapon.baseStats.Interval : powerup.baseStats.Interval;
                           if(weapon._firedSoFar >= amount) {
                               ResetWeapon(ref powerup, ref weapon, pCdr);
                           }
                           else if(weapon._intervalTick >= interval) {
                               weapon._intervalTick = 0;
                           }
                           break;
                   }
                   var wc = weaponBuffer[i];
                   wc.Weapon = weapon;
                   weaponBuffer[i] = wc;
                   puc.PowerUp = powerup;
               }
           }).Schedule(Dependency);
        
        // Reset flags
        if(PlayerHitFlag[0]) {
            PlayerHitFlag[0] = false;
        }
    }

    private static void SetMissileData(MissileAspect missile, in PowerUp powerup, in ID missileID, in MissileArchetype archetype) {
        // set data
        missile.Id.ValueRW.Guid             = missileID.Guid;
        missile.State.ValueRW.RecycleCount  = missile.State.ValueRO.RecycleCount; // We want to remember the RecycleCount of this Missile Entity even after spawning
        missile.Missile.ValueRW.weaponIndex = powerup.weaponIndex;
        missile.Missile.ValueRW.Duration    = archetype.missile.Duration;
        missile.Gfx.ValueRW                 = archetype.gfx;
        missile.Movement.ValueRW            = archetype.movement;

        // TODO: Would be nice to automate this.
        // Increase the Duration of Clockstop.
        var statusLength = missile.Missile.ValueRW.StatusEffects.Length;
        if(statusLength > 0) {
            for(var k = 0; k < statusLength; k++) {
                var status = missile.Missile.ValueRW.StatusEffects[k];
                if(status.HitEffect == HitEffect.Freeze) {
                    status.Duration += powerup.baseStats.FreezeTime;
                }
                missile.Missile.ValueRW.StatusEffects[k] = status;
            }
        }

        // set piercing
        if((missile.Missile.ValueRW.Flags & MissileFlags.IsPiercing) != 0) {
            missile.Missile.ValueRW.Piercing = (sbyte)(missile.Missile.ValueRW.Piercing == 0 ? 1 + powerup.baseStats.Piercing : missile.Missile.ValueRW.Piercing + powerup.baseStats.Piercing);
        }

        // set scale
        if((missile.Missile.ValueRW.Flags & MissileFlags.IgnoreScaling) == 0) {
            missile.Transform.ValueRW.Scale = missile.Missile.ValueRW.ScaleEnd * missile.Missile.ValueRO.Radius; // @ScaleIssue: magic math
            if((missile.Missile.ValueRW.Flags & MissileFlags.GrowsOnBirth) != 0) {
                missile.Transform.ValueRW.Scale = 0.01f;
            }
        }
    }

    static void ResetWeapon(ref PowerUp powerup, ref Weapon weapon, in float pCdr) {
        //var pCdr = PlayerStats.Get(Stats.CooldownReductionID).value;
        var cooldown = math.max(0, PowerUp.GetCooldown(powerup, pCdr));

        switch(powerup.PowerUpType) {
            case PowerUpType.ChargedBuff:
                powerup.baseStats.Charges += 1;
                weapon._cooldownTimer = cooldown;
                break;
        }

        if(weapon.missileData.SpawnDirection == Directions.TowardsFamiliarDamageZone && powerup.familiarTargetEntity != Entity.Null) { // @Familiar
            powerup.familiarTargetFlag = false;
        }

        switch(weapon.PowerUpShootType) {
            case ShootType.BatchedMissiles:
                weapon._firedSoFar = 0;
                weapon._cooldownTimer = cooldown;
                break;

            case ShootType.Interval:
                weapon._intervalTick = 0;
                weapon._firedSoFar = 0;
                weapon.targetDirection = new float3(-1);
                var weaponCooldown = !GrowthStats.IsDefault(weapon.baseStats) ? weapon.baseStats.Cooldown : cooldown;
                //var weaponCooldown = weapon.baseStats != null ? weapon.baseStats.Cooldown : cooldown;
                weapon._cooldownTimer = weaponCooldown;
                break;
        }
        //Debug.Log($"cooldown: {cooldown}, cooldownBuffed: {cooldownBuffed}, cooldownTimer: {powerup._cooldownTimer}");
    }

    static void PlayMissileLaunchForHitEffect(HitEffect hitEffect) {
        AudioClip clip = null;
        switch(hitEffect) {
            case HitEffect.NONE:
                break;
            case HitEffect.Knockback:
            case HitEffect.Normal:
                clip = normalWeapon;
                break;
            case HitEffect.Freeze:
            case HitEffect.Fire:
                clip = magicWeapon;
                break;
            case HitEffect.Electric:
                break;
        }
        
        if(clip == null)
            return;

        AudioSource.PlayOneShot(clip);
        //Debug.Log("playing launch missile clip");
    }

    static ushort GetAmount(in float pAmount, in PowerUp powerup, in Weapon weapon) {
        var powerupAmount = powerup.baseStats.Amount;
        var weaponAmount = !Weapon.IsDefault(weapon) ? !GrowthStats.IsDefault(weapon.baseStats) ? weapon.baseStats.Amount : 0 : 0;
        //var weaponAmount = weapon.baseStats != null ? weapon.baseStats.Amount : 0;

        return (ushort)(pAmount + powerupAmount + weaponAmount);
    }

    static void SetTargetAndDirAnglePosition(ref float3 Direction, ref float3 Target, in ushort firedTotal, in float3 missilePosition, in float radius = 3f) {
        var index = firedTotal % 12;
        var a = index * 360f / 12;
        var dir = AngleToDirection(-a) * radius;
        Direction = math.normalizesafe(dir - missilePosition);
        Target = dir;
    }

    public static float3 AngleToDirection(in float angle) {
        return new float3 {
            x = math.cos(math.radians(angle)),
            y = math.sin(math.radians(angle)),
        };
    }

    static void SetDirectionFromAngles(ref float3 Direction, in float3 Target, in ushort firedSoFar, in float3 position, in NativeArray<float> angles) {
        Direction.xy = math.normalizesafe(Target.xy - position.xy);
        var atan2 = math.atan2(Direction.y, Direction.x);
        var _angle = math.degrees(atan2) + angles[firedSoFar % angles.Length];
        Direction = AngleToDirection(_angle);
    }
}