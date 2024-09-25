using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

public enum PickUpClass {
    None,
    Gold,
    Magnet,
    Heal,
    Rosary,
}

public struct PickUpArchetype {
    public ID id;
    public Gfx gfx;
    public PickUp pickUp;
}

public struct PickUp : IComponentData {
    public PickUpClass Class;
    public FixedString64Bytes GivePowerUpName;
    public StatIncrease StatIncrease;
    public float value;
    public float rarity;
    public bool usesLuck;
    public int unlocksAt;
    public float _accumulatedWeight;
    public float spriteScale;
}

public static partial class PickUps {

    public static NativeHashMap<Guid, PickUpArchetype> Table;
    public static NativeList<ID> ids;
    static NativeList<PickUpArchetype> _pickups;
    
    public static void InitTables() {

        var fields = typeof(PickUps).GetFields();
        InitializerHelper.AddToList(ref fields, ref ids);
        
        var methods = typeof(PickUps).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        InitializerHelper.AddToList(ref methods, ref _pickups);

        UnityEngine.Debug.Assert(ids.Length == _pickups.Length, $"Assertion Failed!, ids: {ids.Length}, pickups: {_pickups.Length}");

        Table = new(ids.Length, Allocator.Persistent);

        for(var i = 0; i < ids.Length; i++) {
            var arch = _pickups[i];

            if(arch.pickUp.Equals(default(PickUp))
                || arch.gfx.Equals(default(Gfx))
                // TODO: check other components. Would be nice to automate this somehow.
                ) {
                UnityEngine.Debug.LogError($"PickUpArchetype default issue, pickup: {arch.pickUp.Equals(default(PickUp))}, gfx: {arch.gfx.Equals(default(Gfx))}!");
                continue;
            }
            // patch ID's so we don't have to manually set the IDs when creating types (PickUp.Clover.cs, etc)
            arch.id = ids[i];
            _pickups[i] = arch;
            Table.Add(ids[i].Guid, _pickups[i]);
            //UnityEngine.Debug.Log($"stat increase guid: ({GfxTable[ids[i].Guid].spriteName}) ({Table[ids[i].Guid].StatIncrease.statType})");
            //UnityEngine.Debug.Log($"adding missile ({gfxes[i].spriteName}) ({ids[i].Guid}) to the MissileTable!");
        }
        _pickups.Dispose();
    }
    public static void Dispose() {
        if(Table.IsCreated) Table.Dispose();
        if(ids.IsCreated) ids.Dispose();
    }
}