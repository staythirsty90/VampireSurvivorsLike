using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class EnemySystem : SystemBase {
    public NativeList<Enemy> DyingEnemies;
    EntityLimits EntityLimits;
    StageManager StageManager;

    protected override void OnStartRunning() {
        base.OnStartRunning();
        EntityLimits = UnityEngine.Object.FindObjectOfType<EntityLimits>();
        UnityEngine.Debug.Assert(EntityLimits);
        StageManager = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<StageManager>();
        UnityEngine.Debug.Assert(StageManager != null);
        if(!DyingEnemies.IsCreated) DyingEnemies = new NativeList<Enemy>(EntityLimits.GetAllEnemiesCount(), Allocator.Persistent);
    }

    protected override void OnUpdate() {
        var dt              = UnityEngine.Time.deltaTime;
        var outer_max       = ScreenBounds.Outer_Max_XY;
        var random          = new Random((uint)UnityEngine.Random.Range(1, 100_000));
        var stageSpawnType  = StageManager.CurrentStage.spawningType;
        var inner           = ScreenBounds.InnerRect;
        var outer           = ScreenBounds.OuterRect;

        // attack
        var playerPos       = float3.zero;
        var chara           = SystemAPI.GetSingleton<CharacterComponent>().character;
        var armor           = chara.CharacterStats.Get(Stats.ArmorID).value;
        var hitflag         = World.GetExistingSystemManaged<MissileSpawnSystem>().PlayerHitFlag;
        var heal            = World.GetExistingSystemManaged<PlayerHealSystem>().requestHeal;
        var hasInvul        = new NativeArray<bool>(1, Allocator.TempJob);
        var skipDamage      = new NativeArray<bool>(1, Allocator.TempJob);
        var DivineID        = Missiles.DivineShieldID.Guid;

        Entities
            .WithNativeDisableParallelForRestriction(skipDamage)
            .ForEach((in PowerUpComponent puc, in DynamicBuffer<WeaponComponent> wbuffer) => {
                // Don't actually take damage if we have at lease one charge of Divine Shield. We just want to set
                // the Player Hit Flag without taking the damage otherwise Divine Shield will only trigger
                // after taking damage first, which could result in player death.

                // TODO: I dislike the disparity here. The Divine Shield missile may not actually spawn if the game
                // is at Missile capacity and this skipDamage flag will still trigger.
                if(puc.PowerUp.PowerUpType == PowerUpType.ChargedBuff && puc.PowerUp.baseStats.Charges > 0 ) {
                    foreach(var wc in wbuffer) {
                        if(wc.Weapon.missileData.missileID.Guid == DivineID) {
                            skipDamage[0] = true;
                        }
                    }
                }
            }).ScheduleParallel();

        Entities
            .WithNativeDisableParallelForRestriction(hasInvul)
            .ForEach((in Missile m, in State state) => {
                if(!state.isActive) return;
                if(state.isDying) return;
                if((m.Flags & MissileFlags.MakesPlayerInvulnerable) == 0) return;

                hasInvul[0] = true;

            }).ScheduleParallel();

        Entities
            .WithReadOnly(hasInvul)
            .WithReadOnly(skipDamage)
            .WithDisposeOnCompletion(hasInvul)
            .WithDisposeOnCompletion(skipDamage)
            .WithNativeDisableParallelForRestriction(hitflag)
            .WithNativeDisableParallelForRestriction(heal)
            .ForEach((EnemyAspect enemy) => {
                if(!enemy.State.ValueRO.isActive) return;
                if(enemy.State.ValueRO.isDying) return;
                if(enemy.Enemy.ValueRO.Damage <= 0) return;
                if(Utils.IsLocked(enemy.State.ValueRO.StatusEffects)) return;

                enemy.Enemy.ValueRW.AttackCooldown -= dt;

                if(enemy.Enemy.ValueRO.AttackCooldown <= 0) {
                    enemy.Enemy.ValueRW.AttackCooldown = enemy.Enemy.ValueRO.AttackRate;
                    if(hasInvul[0]) return;
                    var damageTaken = new NativeArray<float>(1, Allocator.Temp);
                    var pos = new float2(playerPos.x, playerPos.y);
                    var dist = math.distancesq(enemy.Transform.ValueRO.Position.xy, pos);
                    if(dist < enemy.Enemy.ValueRO.AttackRange * enemy.Enemy.ValueRO.AttackRange) {
                        if(!skipDamage[0]) {
                            damageTaken[0] += math.max(1, enemy.Enemy.ValueRO.Damage - armor);
                        }
                        hitflag[0] = true;
                    }
                    heal[0] -= damageTaken[0];
                }
            }).ScheduleParallel();

        // despawn enemies outside of ScreenBounds
        Entities
            .WithName("DespawnEnemies")
            .ForEach((EnemyAspect enemy, in BoundingBox bb) => {
                if(!enemy.State.ValueRO.IsActiveAndNotDying()) return;
                if(Utils.IsLocked(enemy.State.ValueRO.StatusEffects)) return;
                if(enemy.State.ValueRO.readyToActivate) return;
                if((enemy.Enemy.ValueRO.Flags & EnemyFlags.Wall) != 0) return;
                if((enemy.Enemy.ValueRO.specType == Enemy.SpecType.Swarmer)) return;

                if(ScreenBounds.IsOutsideXY(outer_max.x, outer_max.y, in bb)) {
                    enemy.State.ValueRW.doRecycle = true;
                }
            }).ScheduleParallel();

        // respawn elites
        Entities
            .WithName("RespawnElites")
            .ForEach((ref LocalTransform ltf, in Enemy enemy, in BoundingBox bb, in State state) => {
                if(!state.IsActiveAndNotDying()) return;
                if(Utils.IsLocked(state.StatusEffects)) return;
                if(state.readyToActivate) return;
                if(enemy.specType != Enemy.SpecType.Elite) return;

                if(ScreenBounds.IsOutsideXY(outer_max.x, outer_max.y, in bb)) {
                    ltf.Position = ScreenBounds.GetPositionOutOfSight(stageSpawnType, inner, outer, ref random, bb.size);
                }

            }).ScheduleParallel();

        // dying enemies
        var dyingEnemies = DyingEnemies;
        var parallel = dyingEnemies.AsParallelWriter();

        Dependency = Entities
            .WithName("OnEnemiesDied")
            .WithNativeDisableParallelForRestriction(parallel)
            .ForEach((EnemyAspect enemy) => {
                if(enemy.State.ValueRO.isActive && enemy.Enemy.ValueRO.Health <= 0) {
                    if(!enemy.State.ValueRO.isDying) {
                        // NOTE on enemy dies
                        enemy.State.ValueRW.isDying = true;
                        enemy.Transform.ValueRW.Position.z = -3;
                        if((enemy.Enemy.ValueRO.Flags & EnemyFlags.IgnoreKillCount) == 0) {
                            parallel.AddNoResize(enemy.Enemy.ValueRO);
                        }
                    }
                }
            }).ScheduleParallel(Dependency);

        // timed life
        Entities
            .WithName("TimedLifeEnemies")
            .ForEach((EnemyAspect enemy) => {
                if((enemy.Enemy.ValueRO.Flags & EnemyFlags.TimedLife) == 0) return;
                enemy.Enemy.ValueRW.timedLife -= dt;
                if(enemy.Enemy.ValueRO.timedLife <= 0) {
                    enemy.State.ValueRW.doRecycle = true;
                    //UnityEngine.Debug.Log($"needs reset is true, timedLife: {enemy.timedLife}");
                }
        }).ScheduleParallel();

        // rotate enemies
        Entities
            .WithName("RotateEnemies")
            .WithNone<Destructible>()
            .ForEach((EnemyAspect enemy) => {
                if(enemy.Enemy.ValueRO.specType == Enemy.SpecType.Swarmer) return;
                if(enemy.State.ValueRO.isActive && !enemy.State.ValueRO.isDying && !Utils.IsLocked(enemy.State.ValueRO.StatusEffects)) {
                    enemy.Transform.ValueRW.Rotation = new quaternion(quaternion.identity.value.x, quaternion.identity.value.y, quaternion.RotateZ(math.sin((enemy.State.ValueRO.angle) * 5f) * 0.1f).value.z, quaternion.identity.value.w);
                    enemy.State.ValueRW.angle += 1 * dt;
                }
        }).ScheduleParallel();

        // spriteScale enemies
        Entities
            .WithName("ScaleDyingEnemies")
            .ForEach((EnemyAspect enemy) => {
                if(enemy.State.ValueRO.isActive && enemy.State.ValueRO.isDying) {
                    enemy.Transform.ValueRW.Scale -= 3f * dt;
                    if(enemy.Transform.ValueRO.Scale < 0.001f) {
                        enemy.Transform.ValueRW.Scale = 0.001f;
                        if(enemy.Enemy.ValueRW._resetTimer == 0) {
                            //enemy.needsReset = true;
                            enemy.Enemy.ValueRW._resetTimer = 0.05f;
                        }
                        else {
                            enemy.Enemy.ValueRW._resetTimer -= dt;
                            if(enemy.Enemy.ValueRW._resetTimer <= float.Epsilon) {
                                enemy.State.ValueRW.doRecycle = true;
                            }
                        }
                    }
                }
        }).ScheduleParallel();
    }
}