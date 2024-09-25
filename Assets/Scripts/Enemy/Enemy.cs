using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Reflection;
using Unity.Transforms;

[Flags]
public enum EnemyFlags : byte {
    None            = 1 << 0,
    Wall            = 1 << 1,
    TimedLife       = 1 << 2,
    IgnoreKillCount = 1 << 3,
}

public struct Drops : IComponentData {
    public bool DropsPickups;
    public bool DropsXPGems;
    public bool DropsTreasure;
    public bool _didDrop;
    public bool _droppedTreasure;

    public void Reset() {
        _didDrop = false;
        _droppedTreasure = false;
    }
}

public struct EnemyArchetype {
    public ID id;
    public Gfx gfx;
    public Enemy enemy;
    public Drops drops;
    public NonUniformScale spriteScale;
    public OffsetMovement offsetMovement;
    public Movement movement;
}

public readonly partial struct EnemyAspect : IAspect {
    public readonly Entity Self;
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRW<ID> Id;
    public readonly RefRW<Gfx> Gfx;
    public readonly RefRW<Enemy> Enemy;
    public readonly RefRW<Drops> Drops;
    public readonly RefRW<State> State;
    public readonly RefRW<NonUniformScale> SpriteScale;
    public readonly RefRW<OffsetMovement> OffsetMovement;
    public readonly RefRW<Movement> Movement;
}

public struct Enemy : IComponentData {
    public enum SpecType : byte {
        Normal,
        Elite,
        Swarmer,
    }

    public byte Level;
    public ushort Health;
    public ushort MaxHealth;
    public byte Damage;
    public byte XPGiven;
    public float AttackCooldown;
    public float AttackRate;
    public byte AttackRange;
    public byte MoveSpeed;
    public SpecType specType;
    public EnemyFlags Flags;
    public float timedLife;
    public float _resetTimer;

    public void Reset() {
        AttackCooldown = AttackRate;
        Health = MaxHealth;
        Flags = EnemyFlags.None;
        timedLife = -1;
        _resetTimer = 0;
    }
}

public static partial class Enemies {

    public static NativeHashMap<Guid, EnemyArchetype> Table;
    static NativeList<ID> _ids;
    static NativeList<EnemyArchetype> _enemies;

    public static void InitTables() {
        var type = typeof(Enemies);

        var fields = type.GetFields();
        InitializerHelper.AddToList(ref fields, ref _ids);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        InitializerHelper.AddToList(ref methods, ref _enemies);

        Debug.Assert(_ids.Length == _enemies.Length, $"Assertion Failed!, ids: {_ids.Length}, enemies: {_enemies.Length}");

        Table = new(_ids.Length, Allocator.Persistent);

        for(var i = 0; i < _ids.Length; i++) {
            var enemy = _enemies[i];
            if(enemy.enemy.Equals(default(Enemy))
                || enemy.gfx.Equals(default(Gfx))
                || enemy.drops.Equals(default(Drops))
                || enemy.drops.Equals(default(NonUniformScale))
                || enemy.drops.Equals(default(OffsetMovement))
                // NOTE: Check for components where the default is not desired. Would be nice to automate that.
                ) {
                Debug.LogError($"EnemyArchetype default issue (True is bad), enemy: " +
                    $"{enemy.enemy.Equals(default(Enemy))}, " +
                    $"gfx: {enemy.gfx.Equals(default(Gfx))}, " +
                    $"spriteScale: {enemy.spriteScale.Equals(default(NonUniformScale))}, " +
                    $"drops: {enemy.drops.Equals(default(Drops))}, " +
                    $"offsetmovement: {enemy.offsetMovement.Equals(default(OffsetMovement))}!"
                    );
               // continue;
            }
            // patch ID's so we don't have to manually set the IDs when creating types (Enemy.Skeleton.cs, etc)
            enemy.id = _ids[i];

            Table.Add(_ids[i].Guid, enemy);
            _enemies[i] = enemy;
            //Debug.Log("adding enemy archetype to the table!");
        }

        // At the moment we don't use these anymore.
        _ids.Dispose();
        _enemies.Dispose();
    }

    public static void Dispose() {
        if(Table.IsCreated) Table.Dispose();
    }
}