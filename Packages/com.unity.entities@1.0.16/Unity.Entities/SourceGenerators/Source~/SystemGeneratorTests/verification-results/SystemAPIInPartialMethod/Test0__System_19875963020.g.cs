#pragma warning disable 0219
#line 1 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Tests;

[global::System.Runtime.CompilerServices.CompilerGenerated]
public unsafe partial struct PartialMethodSystem : global::Unity.Entities.ISystem, global::Unity.Entities.ISystemCompilerGenerated
{
    [global::Unity.Entities.DOTSCompilerPatchedMethod("CustomOnUpdate_ref_Unity.Entities.SystemState")]
    void __CustomOnUpdate_462BE639(ref SystemState state)
    {
        #line 10 "/0/Test0.cs"
        var tickSingleton2 = __query_1641826531_0.GetSingleton<EcsTestData>();
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
        __query_1641826531_0 = state.GetEntityQuery(new global::Unity.Entities.EntityQueryDesc{All = new global::Unity.Entities.ComponentType[]{global::Unity.Entities.ComponentType.ReadOnly<global::Unity.Entities.Tests.EcsTestData>()}, Any = new global::Unity.Entities.ComponentType[]{}, None = new global::Unity.Entities.ComponentType[]{}, Disabled = new global::Unity.Entities.ComponentType[]{}, Absent = new global::Unity.Entities.ComponentType[]{}, Options = global::Unity.Entities.EntityQueryOptions.Default | global::Unity.Entities.EntityQueryOptions.IncludeSystems});
    }

    public void OnCreateForCompiler(ref SystemState state)
    {
        __AssignQueries(ref state);
        __TypeHandle.__AssignHandles(ref state);
    }
}