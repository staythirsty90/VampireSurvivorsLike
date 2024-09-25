#pragma warning disable 0219
#line 1 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Tests;
using static Unity.Entities.SystemAPI;

[global::System.Runtime.CompilerServices.CompilerGenerated]
public partial struct RotationSpeedSystemForEachISystem : global::Unity.Entities.ISystem, global::Unity.Entities.ISystemCompilerGenerated
{
    [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate_ref_Unity.Entities.SystemState")]
    void __OnUpdate_6E994214(ref SystemState state)
    {
        #line 16 "/0/Test0.cs"
        Entity entity = default;
            #line 17 "/0/Test0.cs"
            global::Unity.Entities.Tests.EcsTestAspect.CompleteDependencyBeforeRW(ref state);
            #line hidden
            __TypeHandle.__Unity_Entities_Tests_EcsTestAspect_RW_AspectLookup.Update(ref state);
            #line hidden
            var testAspectRO = __TypeHandle.__Unity_Entities_Tests_EcsTestAspect_RW_AspectLookup[entity];
    }

    TypeHandle __TypeHandle;
    struct TypeHandle
    {
        public global::Unity.Entities.Tests.EcsTestAspect.Lookup __Unity_Entities_Tests_EcsTestAspect_RW_AspectLookup;
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref global::Unity.Entities.SystemState state)
        {
            __Unity_Entities_Tests_EcsTestAspect_RW_AspectLookup = new global::Unity.Entities.Tests.EcsTestAspect.Lookup(ref state);
        }
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    void __AssignQueries(ref global::Unity.Entities.SystemState state)
    {
    }

    public void OnCreateForCompiler(ref SystemState state)
    {
        __AssignQueries(ref state);
        __TypeHandle.__AssignHandles(ref state);
    }
}