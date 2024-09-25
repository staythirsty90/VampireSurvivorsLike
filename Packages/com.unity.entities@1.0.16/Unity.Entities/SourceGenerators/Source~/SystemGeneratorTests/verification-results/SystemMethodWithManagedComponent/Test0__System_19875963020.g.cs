#pragma warning disable 0219
#line 1 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Tests;
using static Unity.Entities.SystemAPI;

[global::System.Runtime.CompilerServices.CompilerGenerated]
public partial struct SomeSystem : global::Unity.Entities.ISystem, global::Unity.Entities.ISystemCompilerGenerated
{
    [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate_ref_Unity.Entities.SystemState")]
    void __OnUpdate_6E994214(ref SystemState state)
    {
        #line 10 "/0/Test0.cs"
        var e = state.EntityManager.CreateEntity();
        #line 11 "/0/Test0.cs"
        state.EntityManager.AddComponentData(e, new EcsTestManagedComponent{value = "cake"});
        #line 12 "/0/Test0.cs"
        var comp = __query_1641826531_0.GetSingleton<EcsTestManagedComponent>().value;
    }

    TypeHandle __TypeHandle;
    global::Unity.Entities.EntityQuery __query_1641826531_0;
    struct TypeHandle
    {
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref global::Unity.Entities.SystemState state)
        {
        }
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    void __AssignQueries(ref global::Unity.Entities.SystemState state)
    {
        __query_1641826531_0 = state.GetEntityQuery(new global::Unity.Entities.EntityQueryDesc{All = new global::Unity.Entities.ComponentType[]{global::Unity.Entities.ComponentType.ReadOnly<global::Unity.Entities.Tests.EcsTestManagedComponent>()}, Any = new global::Unity.Entities.ComponentType[]{}, None = new global::Unity.Entities.ComponentType[]{}, Disabled = new global::Unity.Entities.ComponentType[]{}, Absent = new global::Unity.Entities.ComponentType[]{}, Options = global::Unity.Entities.EntityQueryOptions.Default | global::Unity.Entities.EntityQueryOptions.IncludeSystems});
    }

    public void OnCreateForCompiler(ref SystemState state)
    {
        __AssignQueries(ref state);
        __TypeHandle.__AssignHandles(ref state);
    }
}