#pragma warning disable 0219
#line 1 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
using Unity.Entities;

[global::System.Runtime.CompilerServices.CompilerGenerated]
partial class EntitiesForEachDynamicBuffer : global::Unity.Entities.SystemBase
{
    [global::Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate")]
    void __OnUpdate_1817F1CB()
    {
        #line 10 "/0/Test0.cs"
        EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Execute();
    }

    #line 16 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
    [global::Unity.Burst.NoAlias]
    [global::Unity.Burst.BurstCompile]
    struct EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job : global::Unity.Entities.IJobChunk
    {
        internal static global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate FunctionPtrFieldNoBurst;
        internal static global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate FunctionPtrFieldBurst;
        public BufferTypeHandle<BufferData> __bufTypeHandle;
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void OriginalLambdaBody(DynamicBuffer<BufferData> buf)
        {
        }

        #line 29 "Temp/GeneratedCode/TestProject/Test0__System_19875963020.g.cs"
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        public void Execute(in global::Unity.Entities.ArchetypeChunk chunk, int batchIndex, bool useEnabledMask, in global::Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            var bufAccessor = chunk.GetBufferAccessor(ref __bufTypeHandle);
            int chunkEntityCount = chunk.Count;
            if (!useEnabledMask)
            {
                for (var entityIndex = 0; entityIndex < chunkEntityCount; ++entityIndex)
                {
                    OriginalLambdaBody(bufAccessor[entityIndex]);
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
                            OriginalLambdaBody(bufAccessor[entityIndex]);
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
                            OriginalLambdaBody(bufAccessor[entityIndex]);
                        }

                        mask64 >>= 1;
                    }

                    mask64 = chunkEnabledMask.ULong1;
                    for (var entityIndex = 64; entityIndex < chunkEntityCount; ++entityIndex)
                    {
                        if ((mask64 & 1) != 0)
                        {
                            OriginalLambdaBody(bufAccessor[entityIndex]);
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
                global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface.RunWithoutJobsInternal(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job>(jobPtr), ref query);
            }
            finally
            {
            }
        }
    }

    void EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Execute()
    {
        __TypeHandle.__BufferData_RW_BufferTypeHandle.Update(ref this.CheckedStateRef);
        var __job = new EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job{__bufTypeHandle = __TypeHandle.__BufferData_RW_BufferTypeHandle};
        if (!__query_1641826531_0.IsEmptyIgnoreFilter)
        {
            this.CheckedStateRef.CompleteDependency();
            var __functionPointer = global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled ? EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.FunctionPtrFieldBurst : EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.FunctionPtrFieldNoBurst;
            global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeRunJobChunk(ref __job, __query_1641826531_0, __functionPointer);
        }
    }

    TypeHandle __TypeHandle;
    global::Unity.Entities.EntityQuery __query_1641826531_0;
    struct TypeHandle
    {
        public Unity.Entities.BufferTypeHandle<global::BufferData> __BufferData_RW_BufferTypeHandle;
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref global::Unity.Entities.SystemState state)
        {
            __BufferData_RW_BufferTypeHandle = state.GetBufferTypeHandle<global::BufferData>(false);
        }
    }

    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    void __AssignQueries(ref global::Unity.Entities.SystemState state)
    {
        __query_1641826531_0 = state.GetEntityQuery(new global::Unity.Entities.EntityQueryDesc{All = new global::Unity.Entities.ComponentType[]{global::Unity.Entities.ComponentType.ReadWrite<global::BufferData>()}, Any = new global::Unity.Entities.ComponentType[]{}, None = new global::Unity.Entities.ComponentType[]{}, Disabled = new global::Unity.Entities.ComponentType[]{}, Absent = new global::Unity.Entities.ComponentType[]{}, Options = global::Unity.Entities.EntityQueryOptions.Default});
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref this.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.FunctionPtrFieldNoBurst = EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.RunWithoutJobSystem;
        EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.FunctionPtrFieldBurst = Unity.Entities.Internal.InternalCompilerInterface.BurstCompile(EntitiesForEachDynamicBuffer_2C1FEB3C_LambdaJob_0_Job.FunctionPtrFieldNoBurst);
    }
}