using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Physics.Extensions;

[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
public struct MissileCollisionJob : IJobParallelFor {
    [ReadOnly]
    public NativeArray<BoundingBox> enemy_bbs;
    [ReadOnly]
    public NativeArray<BoundingBox> missile_bbs;
    [ReadOnly]
    public NativeArray<State> missile_states;
    [ReadOnly]
    public NativeArray<LocalTransform> enemy_transforms;
    [ReadOnly]
    public NativeArray<LocalTransform> missile_transforms;
    [ReadOnly]
    public NativeArray<Missile> missiles;
    [ReadOnly]
    public NativeArray<PhysicsCollider> enemy_colliders;
    [ReadOnly]
    public NativeArray<Entity> enemy_entities;
    [ReadOnly]
    public NativeArray<Entity> missile_entities;
    [NativeDisableParallelForRestriction]
    public BufferLookup<MissilesThatHitMe> missile_buffer;
    [ReadOnly]
    public bool CheckingForObstructibles;

    public unsafe void Execute(int i) {
        //Debug.Log($"checking enemy, i: {i}");
        if(enemy_transforms.Length == 0) {
            //Debug.Log($"No enemies, returning.");
            return;
        }

        //Debug.Log($"Checking enemy, i: {i}");
        var enemyPos            = enemy_transforms[i].Position;
        var enemyEnt            = enemy_entities[i];
        var enemyRadius         = Utils.GetRadius(enemy_colliders[i].ColliderPtr);
        Debug.Assert(enemyRadius != 0, "Enemy Radius is 0!");
        var enemyColliderPos    = Utils.GetCenter(enemy_colliders[i].ColliderPtr);
        var enemyBB             = enemy_bbs[i];
        var buffer              = missile_buffer[enemyEnt];
        var freeSlotIndex       = 0;

        //Debug.Log($"ENEMY POSITION, i: {i}, Position, {enemyPos}");
        //Debug.Log($"active missiles length (top missiles only for now): {active_missiles.Length}");
        for(var j = 0; j < missiles.Length; j++) {
            if(freeSlotIndex == buffer.Length) break;
            //Debug.Log($"checking ENEMY, i: {i}, MISSILE, j: {j}");
            //Debug.Log($"ENEMY POSITION, i: {i}, Position, {enemyPos}");

            var missBB          = missile_bbs[j];
            var missEnt         = missile_entities[j];
            var missile         = missiles[j];
            var missilePos      = missile_transforms[j].Position;
            var missileState    = missile_states[j];
            var hadCollision    = false;

            if(!CheckingForObstructibles) {
                
                // TODO: Be more thorough with enemy position and its Collider position.

                hadCollision = missile.HitType switch {
                    MissileHitType.AoE_Rect          => Utils.IsOverlappingBox2D(ref enemyBB, ref missBB),
                    MissileHitType.AoE_RectRotation  => Utils.IsCircleOverlappingRotatedBox2D(in missBB, enemyPos.xy + enemyColliderPos.xy, enemyRadius),
                    MissileHitType.AoE_Circle2Rect   => Utils.IsCircleOverlappingBox2D(in enemyBB, missilePos.xy, missile.Radius),
                    MissileHitType.AoE_Circle2Circle => Utils.IsCircleOverlappingCircle(enemyPos.xy, enemyRadius, missilePos.xy, missile.Radius).Item1,
                    _                                => MissileCollisionSystem.CheckCollision(enemyPos, missilePos, enemyRadius * enemyRadius),
                };
            }
            else {
                var obsPos = enemyPos + enemyColliderPos;
                hadCollision = missile.HitType switch {
                    MissileHitType.AoE_RectRotation   => Utils.IsCircleOverlappingRotatedBox2D(in missBB, obsPos.xy, enemyRadius),
                    MissileHitType.AoE_Rect           => Utils.IsCircleOverlappingBox2D(in missBB, obsPos.xy, enemyRadius),
                    MissileHitType.AoE_Circle2Rect    => Utils.IsCircleOverlappingCircle(obsPos.xy, enemyRadius, missilePos.xy, missile.Radius).Item1,
                    MissileHitType.AoE_Circle2Circle  => Utils.IsCircleOverlappingCircle(obsPos.xy, enemyRadius, missilePos.xy, missile.Radius).Item1,
                    _                                 => MissileCollisionSystem.CheckCollision(obsPos, missilePos, enemyRadius * enemyRadius),
                };
            }

            if(hadCollision) {
                var skip = false;
                for(var k = 0; k < buffer.Length; k++) {
                    var be = buffer[k];

                    // @MissileRecycleCount
                    // If a missile with has a larger HitFrequency than its recycle and spawn timers then
                    // any Entities that were hit by it may still be tracked in their Buffer. This is because the
                    // Missile Entity Index may not have changed during a recycle

                    if(be.Missile.Index == missEnt.Index && be.MissileRecycleCount == missileState.RecycleCount) {
                        skip = true;
                        break;
                    }
                }
                if(skip) {
                    //Debug.Log($"skipping. enemy Index: {enemyEnt.Index}, enemy pos: {enemyPos}, missile Index: {missEnt.Index}, missile recycleCount: {missile.RecycleCount}");
                    continue;
                }
                //Debug.Log($"taking collision. enemy Index: {enemyEnt.Index}, enemy pos: {enemyPos}, missile Index: {missEnt.Index}, missile pos: {missilePos}");

                // Find the next free Slot for this Missile
                for(var k = 0; k < buffer.Length; k++) {
                    var be = buffer[k];
                    if(be.Missile.Index == 0) {
                        freeSlotIndex = k;
                        break;
                    }
                }

                //Debug.Log($"taking collision. missile Index: {missEnt.Index}, buffer index: {c}");

                // NOTE: If missile.HitFrequency is 0 then the missile can hit a target many times and no Hit sound effects will play.
                var buffElement                 = buffer[freeSlotIndex];
                var defaultHitFrequency         = 0.33f;
                var HitFrequency                = CheckingForObstructibles ? defaultHitFrequency : missile.HitFrequency <= 0 ? defaultHitFrequency : missile.HitFrequency;
                buffElement.maxT                = HitFrequency;
                buffElement.Missile             = missEnt;
                buffElement.MissileRecycleCount = missileState.RecycleCount;
                buffElement.HitEffect           = missile.HitEffect;
                buffElement.t                   = 0;
                buffer[freeSlotIndex++]         = buffElement;
            }
        }
    }
}

[UpdateInGroup(typeof(Init4))]
public partial class MissileCollisionSystem : SystemBase {
    public void Complete() {
        Dependency.Complete();
    }

    public bool IsCompleted() {
        return Dependency.IsCompleted;
    }

    protected override void OnUpdate() {
        var dt                                  = UnityEngine.Time.deltaTime;
        var time                                = UnityEngine.Time.time;
        const Allocator TJ                      = Allocator.TempJob;
        var nullEnt                             = Entity.Null;
        var random                              = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100_000));
        var AllStates                           = GetComponentLookup<State>();
        var AllMissiles                         = GetComponentLookup<Missile>();
        var AllEnemies                          = GetComponentLookup<Enemy>();
        var AllPositions                        = GetComponentLookup<LocalTransform>(true);
        var MissileBuffers                      = GetBufferLookup<MissilesThatHitMe>();

        var active_missiles                     = new NativeList<Missile>(TJ);
        var active_missiles_entities            = new NativeList<Entity>(TJ);
        var active_missiles_transforms          = new NativeList<LocalTransform>(TJ);
        var active_missile_states               = new NativeList<State>(TJ);
        var active_missile_BBs                  = new NativeList<BoundingBox>(TJ);

        var getActiveMissiles = Entities.WithName("GetActiveMissiles").ForEach((Entity e, in Missile m, in State state, in LocalTransform t, in BoundingBox bb) => {
            if(state.isActive && !state.isDying && (m.Flags & MissileFlags.IsDisarmed) == 0) {
                active_missiles.Add(m);
                active_missiles_entities.Add(e);
                active_missiles_transforms.Add(t);
                active_missile_states.Add(state);
                active_missile_BBs.Add(bb);
            }
        }).Schedule(Dependency);

        var active_enemy_entities               = new NativeList<Entity>(TJ);
        var active_enemy_colliders              = new NativeList<PhysicsCollider>(TJ);
        var active_enemy_transforms             = new NativeList<LocalTransform>(TJ);
        var active_enemy_states                 = new NativeList<State>(TJ);
        var active_enemy_BBs                    = new NativeList<BoundingBox>(TJ);

        var getActiveEnemies = Entities.WithName("GetActiveEnemies").ForEach((Entity e, in Enemy enemy, in State state, in LocalTransform t, in BoundingBox bb, in PhysicsCollider pc) => {
            if(state.isActive && !state.isDying) {
                active_enemy_entities.Add(e);
                active_enemy_transforms.Add(t);
                active_enemy_states.Add(state);
                active_enemy_BBs.Add(bb);
                active_enemy_colliders.Add(pc);
            }
        }).Schedule(Dependency);

        var active_obstructible_entities        = new NativeList<Entity>(TJ);
        var active_obstructible_colliders       = new NativeList<PhysicsCollider>(TJ);
        var active_obstructible_translations    = new NativeList<LocalTransform>(TJ);
        var active_obstructible_states          = new NativeList<State>(TJ);
        var active_obstructible_BBs             = new NativeList<BoundingBox>(TJ);

        var getActiveObstructibles = Entities.WithName("GetActiveObstructibles").ForEach((Entity e, in Obstructible obst, in State state, in LocalTransform t, in BoundingBox bb, in PhysicsCollider pc) => {
            if(state.isActive && !state.isDying) {
                active_obstructible_entities.Add(e);
                active_obstructible_translations.Add(t);
                active_obstructible_states.Add(state);
                active_obstructible_BBs.Add(bb);
                active_obstructible_colliders.Add(pc);
            }
        }).Schedule(Dependency);

        getActiveMissiles.Complete();
        getActiveEnemies.Complete();
        getActiveObstructibles.Complete();

        var collisionJob = new MissileCollisionJob() {
            missiles                            = active_missiles.AsArray(),
            missile_states                      = active_missile_states.AsArray(),
            missile_entities                    = active_missiles_entities.AsArray(),
            missile_transforms                  = active_missiles_transforms.AsArray(),
            missile_bbs                         = active_missile_BBs.AsArray(),
            missile_buffer                      = MissileBuffers,
            enemy_entities                      = active_enemy_entities.AsArray(),
            enemy_transforms                    = active_enemy_transforms.AsArray(),
            enemy_colliders                     = active_enemy_colliders.AsArray(),
            enemy_bbs                           = active_enemy_BBs.AsArray(),
        };
        Dependency = collisionJob.Schedule(active_enemy_entities.Length, 1, Dependency);

        var weapon_damage_table = WeaponDamageTable.Table;

        Dependency = Entities
            .WithName("ProcessMissilesThatHitMe")
            .WithNativeDisableParallelForRestriction(AllMissiles)
            .WithNativeDisableParallelForRestriction(weapon_damage_table)
            .WithReadOnly(AllPositions)
            .ForEach((
                Entity e,
                ref Enemy enemy,
                ref State state,
                ref DynamicBuffer<HitEffectBuffer> hitEffectBuffer,
                ref DynamicBuffer<MissilesThatHitMe> missileBuffer,
                in LocalTransform position) => {
                    if(!state.isActive) return;
                    if(state.isDying) return;
                    var k = 0;

                    var hitEffectBufferArray = hitEffectBuffer.ToNativeArray(Allocator.Temp).AsReadOnly();
                    var missileBufferArray = missileBuffer.ToNativeArray(Allocator.Temp).AsReadOnly();
                    var bounced = false;
                    for(var i = 0; i < missileBufferArray.Length; i++) {
                        var mHit = missileBufferArray[i];
                        if(mHit.t != 0) continue;

                        var me = mHit.Missile;
                        if(me.Index == 0)
                            continue;

                        var m = AllMissiles[me];

                        if(!CanHit(ref m, ref mHit, ref missileBuffer, i)) {
                            continue;
                        }

                        var health = enemy.Health;
                        if(health == 0) {
                            break;
                        }
                        if(health - m.Damage < 0) {
                            health = 0;
                        }
                        else {
                            health -= m.Damage;
                        }
                        enemy.Health = health;

                        m.currentHits += 1; // TODO(Race Condition for Parallel Job)

                        weapon_damage_table[m.weaponIndex] += m.Damage;

                        // NOTE:apply the missile's hit effect
                        var hiteffect = hitEffectBufferArray[k];
                        hiteffect.HitEffect = m.HitEffect;
                        hitEffectBuffer[k] = hiteffect;
                        k++;
                        if(k >= hitEffectBuffer.Length) k = 0;

                        // NOTE Add the missiles status effects to the enemys status effects
                        for(var j = 0; j < m.StatusEffects.Length; j++) {
                            var effect = m.StatusEffects[j];
                            var mpos = AllPositions[me];
                            Utils.AddStatusEffect(ref effect, ref state.StatusEffects, position.Position, mpos.Position);
                        }

                        DeductPierceAndFlip(ref m, MissileFlags.BouncesOffEnemies, ref bounced);

                        AllMissiles[me] = m;
                    }
                }).Schedule(Dependency);

        var obstructiblesJob = new MissileCollisionJob() {
            missiles                        = active_missiles.AsArray(),
            missile_states                  = active_missile_states.AsArray(),
            missile_entities                = active_missiles_entities.AsArray(),
            missile_transforms              = active_missiles_transforms.AsArray(),
            missile_bbs                     = active_missile_BBs.AsArray(),
            missile_buffer                  = MissileBuffers,
            enemy_entities                  = active_obstructible_entities.AsArray(),
            enemy_transforms                = active_obstructible_translations.AsArray(),
            enemy_colliders                 = active_obstructible_colliders.AsArray(),
            enemy_bbs                       = active_obstructible_BBs.AsArray(),
            CheckingForObstructibles        = true,
        };
        Dependency = obstructiblesJob.Schedule(active_obstructible_entities.Length, 1, Dependency);

        Dependency = Entities
            .WithName("ProcessMissilesThatHitMeObstructibles")
            .WithNativeDisableParallelForRestriction(AllMissiles)
            .WithAll<Obstructible>()
            .ForEach((
                Entity e,
                ref State state,
                ref DynamicBuffer<MissilesThatHitMe> missileBuffer) => {
                    if(!state.isActive) return;
                    if(state.isDying) return;

                    var missileBufferArray = missileBuffer.ToNativeArray(Allocator.Temp).AsReadOnly();
                    var bounced = false;

                    for(var i = 0; i < missileBufferArray.Length; i++) {
                        var mHit = missileBufferArray[i];
                        if(mHit.t != 0) continue;

                        var me = mHit.Missile;
                        if(me.Index == 0)
                            continue;

                        var m = AllMissiles[me];

                        if((m.Flags & MissileFlags.BouncesOffObstructibles) == 0) {
                            continue;
                        }

                        if(!CanHit(ref m, ref mHit, ref missileBuffer, i)) {
                            continue;
                        }
                        
                        DeductPierceAndFlip(ref m, MissileFlags.BouncesOffObstructibles, ref bounced);

                        AllMissiles[me] = m;
                    }
                }).ScheduleParallel(Dependency);

        active_missiles                     .Dispose(Dependency);
        active_missile_states               .Dispose(Dependency);
        active_missiles_entities            .Dispose(Dependency);
        active_missiles_transforms          .Dispose(Dependency);
        active_missile_BBs                  .Dispose(Dependency);
        active_enemy_states                 .Dispose(Dependency);
        active_enemy_BBs                    .Dispose(Dependency);
        active_enemy_colliders              .Dispose(Dependency);
        active_obstructible_entities        .Dispose(Dependency);
        active_obstructible_colliders       .Dispose(Dependency);
        active_obstructible_translations    .Dispose(Dependency);
        active_obstructible_states          .Dispose(Dependency);
        active_obstructible_BBs             .Dispose(Dependency);

        var getDamageNumbersDeps = Entities.WithReadOnly(AllMissiles).WithName("ProcessDamageNumbers").ForEach((Entity e, ref DamageNumberIndex dnIndex, ref DynamicBuffer<DamageNumberData> dmgBuffer, in DynamicBuffer<MissilesThatHitMe> missilesBuffer, in LocalTransform position) => {
            var damage = 0;
            var missilesBufferArray = missilesBuffer.ToNativeArray(Allocator.Temp).AsReadOnly();
            for(var j = 0; j < missilesBufferArray.Length; j++) {
                var mHit = missilesBufferArray[j];
                if(mHit.t != 0) continue;
                if(mHit.Missile.Index == 0) continue;
                if(mHit.skip) continue;
                var m = AllMissiles[mHit.Missile];
                if(m.Damage == 0) continue;
                damage += math.max(1, random.NextInt(m.Damage-5, m.Damage+5));
            }

            if(damage == 0) return;

            var idx = DamageNumberIndex.GetNext(ref dnIndex, in dmgBuffer);
            var dmgBufferArray = dmgBuffer.ToNativeArray(Allocator.Temp);
            var dn = dmgBufferArray[idx];
            var finalPos = position.Position;
            dn.position1 = finalPos;

            if(damage < 10) {
                dn.value1 = (byte)damage;
                dn.value2 = 0;
                dn.value3 = 0;
                dn.value4 = 0;
            }
            else if(damage < 100) {
                dn.value1 = (byte)(damage % 10);
                dn.value2 = (byte)(damage / 10);
                dn.value3 = 0;
                dn.value4 = 0;
            }
            else if(damage < 1000) {
                dn.value1 = (byte)(damage % 10);
                dn.value2 = (byte)(damage / 10 % 10);
                dn.value3 = (byte)(damage / 100 % 10);
                dn.value4 = 0;
            }
            // TODO: What about damage thats over 9999?
            else if(damage < 10_000) {
                dn.value1 = (byte)(damage % 10);
                dn.value2 = (byte)(damage / 10 % 10);
                dn.value3 = (byte)(damage / 100 % 10);
                dn.value4 = (byte)(damage / 1000 % 10);
            }
            dn.shouldDraw = true;
            dn.timeAlive = 0;
            dmgBuffer[idx] = dn;
        }).ScheduleParallel(Dependency);
        Dependency = getDamageNumbersDeps;

        var knockback = Entities.WithName("ProcessKnockbacks").ForEach((ref State state, ref PhysicsVelocity velocity, in PhysicsMass mass, in LocalTransform position) => {
            if(!state.isActive) return;
            if(state.isDying) return;

            if(Utils.IsKnockedBack(state.StatusEffects)) {
                var knockback = Utils.GetKnockBack(state.StatusEffects);
                var knockbackDirection = new float3(math.normalizesafe(knockback.xy), 0) * knockback.z * 6f; // TODO: Hard coded numbers.
                velocity.ApplyLinearImpulse(in mass, knockbackDirection);
                Utils.RemoveKnockBack(ref state.StatusEffects);
                //Debug.Log($"enemy has knockback effect: {knockback}");
            }
        }).ScheduleParallel(Dependency);
        Dependency = knockback;

        var statusEffects = Entities.WithAll<Enemy>().WithName("ProcessStatusEffects").ForEach((ref State state) => {
            if(!state.isActive) return;
            if(state.isDying) return;
            var list = state.StatusEffects;
            for(var i = 0; i < list.Length; i++) {
                var effect = list[i];
                effect.Duration -= dt;
                if(effect.Duration < 0) effect.Duration = 0;
                list[i] = effect;
            }
            state.StatusEffects = list;
        }).ScheduleParallel(Dependency);
        Dependency = statusEffects;

        var processMissileHitBuffersDeps = Entities.WithName("ProcessHitBuffers").ForEach((Entity e, ref DynamicBuffer<MissilesThatHitMe> buffer) => {
            var bufferArray = buffer.ToNativeArray(Allocator.Temp).AsReadOnly();
            for(var i = 0; i < bufferArray.Length; i++) {
                var mHit = bufferArray[i];
                if(mHit.Missile.Index == 0) {
                    mHit.t = 0;
                    mHit.Missile = nullEnt;
                    mHit.MissileRecycleCount = 0;
                }
                else {
                    mHit.t += dt;
                }
                if(mHit.t > mHit.maxT) {
                    // NOTE reset MissilesThatHitMe here. don't forget to reset any fields here! perhaps we can just do mHit = new () ?
                    mHit.Missile = nullEnt;
                    mHit.t = 0;
                    mHit.MissileRecycleCount = 0;
                    mHit.maxT = 0.33f;
                    mHit.HitEffect = HitEffect.NONE;
                    mHit.skip = false;
                }
                buffer[i] = mHit;
            }
        }).ScheduleParallel(Dependency);
        Dependency = processMissileHitBuffersDeps;

        // Reset missile's current hits count
        Dependency = Entities.WithName("UpdateMissileCurrentHits").ForEach((ref Missile missile, in State state) => {
            if(!state.isActive) return;
            missile.currentHits = 0;
        }).ScheduleParallel(Dependency);

        active_enemy_entities   .Dispose(getDamageNumbersDeps);
        active_enemy_transforms .Dispose(getDamageNumbersDeps);
    }

    public static bool CheckCollision(float3 posA, float3 posB, float radiusSqr) {
        posA.z = 0;
        posB.z = 0;
        var delta = posA - posB;
        var distanceSquare = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
        //Debug.Log($"distanceSquare: {distanceSquare}");
        return distanceSquare <= radiusSqr + 1f;
    }

    static void DeductPierceAndFlip(ref Missile m, MissileFlags flag, ref bool bounced) {
        if((m.Flags & MissileFlags.IsPiercing) != 0) {
            m.Piercing -= 1; // TODO(Race Condition for Parallel Job): Changing Piercing in parallel. Enemies likely share Missile(s) in MissilesThatHitMe buffers.
            if(m.Piercing <= 0) {
                m.Piercing = 0;
                return;
            }
        }
        if((m.Flags & flag) != 0 && !bounced) { // check bounce flags
            m.direction.x = -m.direction.x;
            m.direction.y = -m.direction.y;
        }
    }

    static bool CanHit(ref Missile m, ref MissilesThatHitMe mHit, ref DynamicBuffer<MissilesThatHitMe> missileBuffer, int index) {
        if(m.maxHits != 0 && m.currentHits >= m.maxHits) {
            //Debug.Log($"SKIPPING currentHits: {m.currentHits}, maxHits:{m.maxHits}");
            mHit.skip = true; // NOTE: Setting this _should_ let other proceeding systems skip this buffer element.
            missileBuffer[index] = mHit;
            return false; // TODO: Could this be buggy? The missile may have been included in several enemy hit buffers but we are only going to process it maxHits number of times.
        }
        //Debug.Log($"NO skip. currentHits: {m.currentHits}, maxHits: {m.maxHits}");
        if((m.Flags & MissileFlags.IsPiercing) != 0 && m.Piercing == 0) {
            //Debug.Log($"Skipping because missile has 0 Piercing!");
            return false;
        }
        return true;
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class AudioTest : SystemBase {

    AudioSource AudioSource;
    AudioClip hiteffectFire;
    AudioClip hiteffectFrozen;
    AudioClip hiteffectElectric;
    AudioClip hiteffectNormal;
    protected override void OnCreate() {
        base.OnCreate();
        AudioSource = GameObject.Find("Audio Source Hit").GetComponent<AudioSource>();
        Debug.Assert(AudioSource);
        hiteffectNormal = Resources.Load<AudioClip>("Audio/HitEffectNormal3");
        hiteffectFire = Resources.Load<AudioClip>("Audio/HitEffectFire2");
        hiteffectFrozen = Resources.Load<AudioClip>("Audio/HitEffectFrozen");
        hiteffectElectric = Resources.Load<AudioClip>("Audio/HitEffectElectric5");
    }

    protected override void OnUpdate() {

        if(!World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MissileCollisionSystem>().IsCompleted())
            return;

        var dt = UnityEngine.Time.deltaTime;
        var normalHits = 0;
        var fireHits = 0;
        var frozenHits = 0;
        var elecHits = 0;

        Entities.WithName("AudioHitBuffers").ForEach((in DynamicBuffer<MissilesThatHitMe> hits) => {
            var length = hits.Length;
            for(var i = 0; i < length; i++) {
                var hit = hits[i];
                if(hit.Missile.Index == 0 || hit.skip) {
                    return;
                }
                switch(hit.HitEffect) {
                    case HitEffect.NONE:
                        break;
                    case HitEffect.Normal:
                        if(hit.t <= dt) {
                            normalHits++;
                        }
                        break;
                    case HitEffect.Freeze:
                        if(hit.t <= dt) {
                            frozenHits++;
                        }
                        break;
                    case HitEffect.Fire:
                        if(hit.t <= dt) {
                            fireHits++;
                        }
                        break;
                    case HitEffect.Electric:
                        if(hit.t <= dt) {
                            elecHits++;
                        }
                        break;
                    case HitEffect.Knockback:
                        break;
                }
            }
        }).Run();
        //Debug.Log($"firecount: {firecount}, firecountNOW: {firecountNOW}");

        AudioSource.pitch = UnityEngine.Random.Range(0.6f, 1.4f);
        AudioSource.volume = UnityEngine.Random.Range(0.7f, 1f);

        if(fireHits > 0) {
            AudioSource.PlayOneShot(hiteffectFire);
        }
        if(frozenHits > 0) {
            AudioSource.PlayOneShot(hiteffectFrozen);
        }
        if(elecHits > 0) {
            AudioSource.PlayOneShot(hiteffectElectric);
        }
        if(normalHits > 0) {
            AudioSource.PlayOneShot(hiteffectNormal);
        }
    }
}