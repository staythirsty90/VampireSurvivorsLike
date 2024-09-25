using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System;
using System.Reflection;
using Unity.Transforms;

[Flags]
public enum MissileFlags {
    None                        = 0 << 0,
    BouncesOffWalls             = 1 << 1,
    ShrinksOnDying              = 1 << 2,
    Flips                       = 1 << 3,
    Spins                       = 1 << 4,
    NoDespawn                   = 1 << 5,
    GrowsOnBirth                = 1 << 6,
    Explodes                    = 1 << 7,
    RotateDirection             = 1 << 8,
    KillAfterAnimation          = 1 << 9,
    IgnoreScaling               = 1 << 10,
    HasTimedLife                = 1 << 11,
    DontUpdateStats             = 1 << 12,
    BouncesOffEnemies           = 1 << 13,
    IsFamiliar                  = 1 << 14,
    IsFamiliarDamageZone        = 1 << 15,
    IsDisarmed                  = 1 << 16,
    FacesCenter                 = 1 << 17,
    BouncesOffObstructibles     = 1 << 18,
    IsPiercing                  = 1 << 19,
    MakesPlayerInvulnerable     = 1 << 20,
    SpeedIncreasesPerShot       = 1 << 21,
    IgnoresSpeed                = 1 << 22,
    IgnoresArea                 = 1 << 23,
    IgnoresDuration             = 1 << 24,
}

public enum MissileHitType : byte {
    Point,
    AoE_Rect,
    AoE_Circle2Rect,
    AoE_Circle2Circle,
    AoE_RectRotation,
}

public enum SpawnPositionType : byte {
    None,
    AtPlayer,
    AtRandomEnemy,
    AtFamiliar,
}

public enum SpawnVariationType : byte {
    None,
    RandomSpreadWithArea_Knife, // Knife,
    RandomSpreadWithArea2_Cross, // Cross,
    RandomSpread, // Magic Wand,
    FlipsUpward, // Whip
    FamiliarRandomSpread,
}

public enum MissileMoveType : byte {
    Forward,
    Spiral,
    Orbit,
    Stationary,
    Boomerang,
    FollowsPlayer,
    Arc,
}

public struct SubMissileDeath {
    public ID missileToSpawnID;
}

public struct PowerUpLink : IComponentData {
    public Entity powerupComponent;
}

public struct MissileArchetype {
    public ID id;
    public Gfx gfx;
    public Missile missile;
    public OffsetMovement movement;
}

public readonly partial struct MissileAspect : IAspect {
    public readonly Entity Self;
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRW<ID> Id;
    public readonly RefRW<Gfx> Gfx;
    public readonly RefRW<Missile> Missile;
    public readonly RefRW<State> State;
    public readonly RefRW<OffsetMovement> Movement;
    public readonly RefRW<BoundingBox> BoundingBox;
    public readonly RefRW<SpriteFrameData> sf_data;
    public readonly RefRW<NonUniformScale> nonUniformScale;
    public readonly RefRW<PowerUpLink> pulink;
}

public struct Missile : IComponentData {
    public HitEffect HitEffect;
    public float HitFrequency;
    public byte currentHits;
    public byte maxHits;
    public sbyte weaponIndex;
    public ushort Damage;
    public float Speed;
    public float Duration;
    public sbyte Piercing;
    public float ScaleEnd;
    public float Radius;
    public ushort firedIndex;
    public float3 target;
    public float3 _spawnedPoint;
    public float3 spawnOffset;
    public float distanceFromCenter;
    public float currentAngle;
    public float spinAngle;
    public float spinSpeedMultiplier;
    public float Acceleration;
    public float _acceleration;
    public MissileFlags Flags;
    public MissileHitType HitType;
    public Directions SpawnDirection;
    public SpawnPositionType SpawnPositionType;
    public SpawnVariationType SpawnVariationType;
    public MissileMoveType MoveType;
    public float3 direction;
    public FixedList128Bytes<StatusEffectApplier> StatusEffects;
}

public static partial class Missiles {

    public static NativeHashMap<Guid, MissileArchetype> MissileTable;

    public static NativeList<ID> ids;
    public static NativeList<MissileArchetype> missiles;

    public static void InitTables() {
        var type = typeof(Missiles);

        var fields = type.GetFields();
        InitializerHelper.AddToList(ref fields, ref ids);
        
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        InitializerHelper.AddToList(ref methods, ref missiles);
        
        UnityEngine.Debug.Assert(ids.Length == missiles.Length, $"Assertion Failed!, ids: {ids.Length}, missiles: {missiles.Length}");

        MissileTable = new(ids.Length, Allocator.Persistent);

        for(var i = 0; i < ids.Length; i++) {
            var arch = missiles[i];

            if(arch.missile.Equals(default(Missile))
                || arch.gfx.Equals(default(Gfx))
                // TODO: Check other components. Would be nice to automate that.
                ) {
                UnityEngine.Debug.LogError($"MissileArchetype default issue, missile: {arch.missile.Equals(default(Missile))}, gfx: {arch.gfx.Equals(default(Gfx))}!");
                continue;
            }

            // patch ID's so we don't have to manually set the IDs when creating types (Missile.Axe.cs, etc)
            arch.id = ids[i];
            missiles[i] = arch;
            MissileTable.Add(ids[i].Guid, missiles[i]);
            //UnityEngine.Debug.Log($"adding missile ({gfxes[i].spriteName}) ({ids[i].Guid}) to the MissileTable!");
        }
    }

    public static void Dispose() {
        if(MissileTable.IsCreated) MissileTable.Dispose();
        if(ids.IsCreated) ids.Dispose();
        if(missiles.IsCreated) missiles.Dispose();
    }
}