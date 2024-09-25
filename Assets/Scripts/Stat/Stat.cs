using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;


[Serializable]
public struct StatIncrease {
    public Guid statType;
    public float value;
    public bool isPercentageBased;
}

[Serializable]
public struct StatIncreaseOnLevel {
    public byte levelInterval;
    public byte maxApplications;
    public StatIncrease StatIncrease;
}

[Serializable]
public struct Stat {
    [HideInInspector]
    public FixedString32Bytes name;
    [HideInInspector]
    public FixedString32Bytes iconName;
    [Range(0, 100)]
    public float value;
    [HideInInspector]
    public bool skipRecalc;
    [HideInInspector]
    public bool invisible;
    public override string ToString() {
        return value.ToString();
    }
}

public static partial class Stats {
    public static Dictionary<Guid, MethodInfo> StatTable = new();
    readonly static List<Guid> ids = new();
    readonly static List<MethodInfo> stats = new();

    public static void InitTables() {

        ids.Clear();
        stats.Clear();
        StatTable.Clear();

        var fields = typeof(Stats).GetFields();

        foreach (var field in fields) {
            if(field.FieldType != typeof(Guid)) {
                continue;
            }

            var id = (Guid)field.GetValue(null);

            if(id.Equals(Guid.Empty)) {
                id = Guid.NewGuid();
                field.SetValue(null, id);
            }

            ids.Add(id);
        }

        var methods = typeof(Stats).GetMethods();
        foreach(var method in methods) {
            if(method.ReturnType != typeof(Stat)) {
                continue;
            }
            stats.Add(method);
        }

        Debug.Assert(ids.Count == stats.Count);

        for(var i = 0; i < ids.Count; i++) {
            StatTable.Add(ids[i], stats[i]);
            //Debug.Log($"Adding stat ({stats[i].Name}) (id:{ids[i]}) to Stat Table");
        }
    }
}