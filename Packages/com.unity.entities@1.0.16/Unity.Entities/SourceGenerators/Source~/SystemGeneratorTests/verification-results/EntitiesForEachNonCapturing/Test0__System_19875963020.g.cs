#pragma warning disable 0219
#line 1 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[global::System.Runtime.CompilerServices.CompilerGenerated]
partial class EntitiesForEachNonCapturing : global::Unity.Entities.SystemBase
{
    [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate")]
    void __OnUpdate_1817F1CB()
    {
        #line 14 "/0/Test0.cs"
        EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Execute();
    }

    #line 18 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
    [global::Unity.Burst.NoAlias]
    [global::Unity.Burst.BurstCompile]
    struct EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job : global::Unity.Entities.IJobChunk
    {
        internal static global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate FunctionPtrFieldNoBurst;
        internal static global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate FunctionPtrFieldBurst;
        public global::Unity.Entities.ComponentTypeHandle<global::Translation> __translationTypeHandle;
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void OriginalLambdaBody([Unity.Burst.NoAlias] ref global::Translation translation, [Unity.Burst.NoAlias] ref global::TagComponent1 tag1, [Unity.Burst.NoAlias] in global::TagComponent2 tag2)
        {
            #line 14 "/0/Test0.cs"
            translation.Value += 5;
        }

        #line 33 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        public void Execute(in global::Unity.Entities.ArchetypeChunk chunk, int batchIndex, bool useEnabledMask, in global::Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var translationArrayPtr = global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetChunkNativeArrayIntPtr<global::Translation>(chunk, ref __translationTypeHandle);
            int chunkEntityCount = chunk.Count;
            if (!useEnabledMask)
            {
                for (var entityIndex = 0; entityIndex < chunkEntityCount; ++entityIndex)
                {
                    global::TagComponent1 tag1 = default;
                    ;
                    global::TagComponent2 tag2 = default;
                    OriginalLambdaBody(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::Translation>(translationArrayPtr, entityIndex), ref tag1, in tag2);
                }
            }
            else
            {
                int edgeCount = global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong0 ^ (chunkEnabledMask.ULong0 << 1)) + global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong1 ^ (chunkEnabledMask.ULong1 << 1)) - 1;
                bool useRanges = edgeCount <= 4;
                if (useRanges)
                {
                    int entityIndex = 0;
                    int batchEndIndex = 0;
                    while (global::Unity.Entities.EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out entityIndex, out batchEndIndex))
                    {
                        while (entityIndex < batchEndIndex)
                        {
                            global::TagComponent1 tag1 = default;
                            ;
                            global::TagComponent2 tag2 = default;
                            OriginalLambdaBody(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::Translation>(translationArrayPtr, entityIndex), ref tag1, in tag2);
                            entityIndex++;
                        }
                    }
                }
                else
                {
                    ulong mask64 = chunkEnabledMask.ULong0;
                    int count = global::Unity.Mathematics.math.min(64, chunkEntityCount);
                    for (var entityIndex = 0; entityIndex < count; ++entityIndex)
                    {
                        if ((mask64 & 1) != 0)
                        {
                            global::TagComponent1 tag1 = default;
                            ;
                            global::TagComponent2 tag2 = default;
                            OriginalLambdaBody(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::Translation>(translationArrayPtr, entityIndex), ref tag1, in tag2);
                        }

                        mask64 >>= 1;
                    }

                    mask64 = chunkEnabledMask.ULong1;
                    for (var entityIndex = 64; entityIndex < chunkEntityCount; ++entityIndex)
                    {
                        if ((mask64 & 1) != 0)
                        {
                            global::TagComponent1 tag1 = default;
                            ;
                            global::TagComponent2 tag2 = default;
                            OriginalLambdaBody(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<global::Translation>(translationArrayPtr, entityIndex), ref tag1, in tag2);
                        }

                        mask64 >>= 1;
                    }
                }
            }
        }

        [global::Unity.Burst.BurstCompile]
        [global::AOT.MonoPInvokeCallback(typeof(global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate))]
        public static void RunWithoutJobSystem(ref global::Unity.Entities.EntityQuery query, global::System.IntPtr jobPtr)
        {
            try
            {
                global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface.RunWithoutJobsInternal(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job>(jobPtr), ref query);
            }
            finally
            {
            }
        }
    }

    void EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Execute()
    {
        __TypeHandle.__Translation_RW_ComponentTypeHandle.Update(ref this.CheckedStateRef);
        var __job = new EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job{__translationTypeHandle = __TypeHandle.__Translation_RW_ComponentTypeHandle};
        if (!__query_1641826535_0.IsEmptyIgnoreFilter)
        {
            this.CheckedStateRef.CompleteDependency();
            var __functionPointer = global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled ? EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.FunctionPtrFieldBurst : EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.FunctionPtrFieldNoBurst;
            global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeRunJobChunk(ref __job, __query_1641826535_0, __functionPointer);
        }
    }

    TypeHandle __TypeHandle;
    global::Unity.Entities.EntityQuery __query_1641826535_0;
    struct TypeHandle
    {
        public Unity.Entities.ComponentTypeHandle<global::Translation> __Translation_RW_ComponentTypeHandle;
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref global::Unity.Entities.SystemState state)
        {
            __Translation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<global::Translation>(false);
        }
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    void __AssignQueries(ref global::Unity.Entities.SystemState state)
    {
        __query_1641826535_0 = state.GetEntityQuery(new global::Unity.Entities.EntityQueryDesc{All = new global::Unity.Entities.ComponentType[]{global::Unity.Entities.ComponentType.ReadOnly<global::TagComponent1>(), global::Unity.Entities.ComponentType.ReadOnly<global::TagComponent2>(), global::Unity.Entities.ComponentType.ReadWrite<global::Translation>()}, Any = new global::Unity.Entities.ComponentType[]{}, None = new global::Unity.Entities.ComponentType[]{}, Disabled = new global::Unity.Entities.ComponentType[]{}, Absent = new global::Unity.Entities.ComponentType[]{}, Options = global::Unity.Entities.EntityQueryOptions.Default});
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref this.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.FunctionPtrFieldNoBurst = EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.RunWithoutJobSystem;
        EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.FunctionPtrFieldBurst = Unity.Entities.Internal.InternalCompilerInterface.BurstCompile(EntitiesForEachNonCapturing_625D99F_LambdaJob_0_Job.FunctionPtrFieldNoBurst);
    }
}