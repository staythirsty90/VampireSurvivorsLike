using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using static Unity.Entities.SourceGen.LambdaJobs.LambdaParamDescription_EntityCommandBuffer;

namespace Unity.Entities.SourceGen.LambdaJobs
{
    static partial class EntitiesSourceFactory
    {
        public static class LambdaJobs
        {
            public static StructDeclarationSyntax JobStructFor(LambdaJobDescription description)
            {
                var template = $@"
			    {TypeCreationHelpers.GeneratedLineTriviaToGeneratedSource}
                {Common.NoAliasAttribute(description)}
                {Common.BurstCompileAttribute(description)}
                {(description.NeedsUnsafe ? "unsafe " : string.Empty)}struct {description.JobStructName}
                {JobInterface()}
                {{
                    {(description.IsForDOTSRuntime ? IJobBaseMethods(description) : string.Empty)}
                    {(description.NeedsEntityInQueryIndex ? ChunkBaseEntityIndicesField() : string.Empty)}
                    {RunWithoutJobSystemDelegateFields(description).EmitIfTrue(description.NeedsJobFunctionPointers)}
                    {StructSystemFields()}
                    {CapturedVariableFields()}
                    {TypeHandleFields().EmitIfTrue(!description.WithStructuralChanges)}
                    {AdditionalDataLookupFields()}
                    {GenerateProfilerMarker(description)}

                    {OriginalLambdaBody()}

                    {ExecuteMethod()}
                    {DisposeOnCompletionMethod()}

                    {RunWithoutJobSystemMethod(description).EmitIfTrue(description.NeedsJobFunctionPointers)}
                }}";

                static string ChunkBaseEntityIndicesField() =>
                    @"[global::Unity.Collections.ReadOnly]
                    public global::Unity.Collections.NativeArray<int> __ChunkBaseEntityIndices;";

                var jobStructDeclaration = (StructDeclarationSyntax) SyntaxFactory.ParseMemberDeclaration(template);

                // Find lambda body in job struct template and replace rewritten lambda body into method
                var templateLambdaMethodBody = jobStructDeclaration.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>().First(
                        method => method.Identifier.ValueText == "OriginalLambdaBody").DescendantNodes()
                    .OfType<BlockSyntax>().First();
                jobStructDeclaration = jobStructDeclaration.ReplaceNode(templateLambdaMethodBody,
                    description.RewrittenLambdaBody.WithoutPreprocessorTrivia());

                    return jobStructDeclaration;

                static string GetChunkNativeArrays(LambdaJobDescription description) =>
                    description.LambdaParameters.Select(param => param.GetNativeArrayOrAccessor()).SeparateByNewLine();

                //var rotationTypeIndex = Unity.Entities.TypeManager.GetTypeIndex<Rotation>();
                static string StructuralChanges_GetTypeIndices(LambdaJobDescription description) =>
                    description.LambdaParameters.Select(param => param.StructuralChanges_GetTypeIndex())
                        .SeparateByNewLine();

                // var rotationOriginal = _rotationLookup[entity]; var rotation = rotationOriginal;
                static string StructuralChanges_ReadLambdaParams(LambdaJobDescription description) =>
                    description.LambdaParameters.Select(param => param.StructuralChanges_ReadLambdaParam())
                        .SeparateByNewLine();

                // UnsafeUnsafeWriteComponentData<Rotation>(__this.EntityManager, entity, rotationTypeIndex, ref rotation, ref T originalrotation);";
                static string StructuralChanges_WriteBackLambdaParams(LambdaJobDescription description) =>
                    description.LambdaParameters.Select(param => param.StructuralChanges_WriteBackLambdaParam())
                        .SeparateByNewLine();

                string StructSystemFields() =>
                    description.NeedsTimeData ? "public global::Unity.Core.TimeData __Time;" : string.Empty;

                // public [ReadOnly] CapturedFieldType capturedFieldName;
                // Need to also declare these for variables used by local methods
                string CapturedVariableFields()
                {
                    static string FieldForCapturedVariable(LambdaCapturedVariableDescription variable) =>
                        $@"{variable.Attributes.JoinAttributes()}public {variable.Symbol.GetSymbolType().ToFullName()} {variable.VariableFieldName};";

                    return description.VariablesCaptured.Select(FieldForCapturedVariable).SeparateByNewLine();
                }

                // public ComponentTypeHandle<ComponentType> _rotationTypeAccessor;
                string TypeHandleFields() => description.LambdaParameters.Select(param => param.FieldInGeneratedJobChunkType())
                    .SeparateByNewLine();

                // public Unity.Entities.ComponentLookup<ComponentType> _rotationLookup;
                string AdditionalDataLookupFields() =>
                    description.AdditionalFields
                        .Select(dataLookupField => dataLookupField.ToFieldDeclaration().ToString())
                        .SeparateByNewLine();

                // void OriginalLambdaBody(ref ComponentType1 component1, in ComponentType2 component2) {}";
                string OriginalLambdaBody() => $@"
                {"[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]".EmitIfTrue(description.Burst.IsEnabled)}
                void OriginalLambdaBody({description.LambdaParameters.Select(param => param.LambdaBodyMethodParameter(description.Burst.IsEnabled)).SeparateByComma()}) {{}}
                {TypeCreationHelpers.GeneratedLineTriviaToGeneratedSource}";

                // OriginalLambdaBody(ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AsRef<ComponentType1>(componentArray1 + i), *(componentArray2 + i));
                string PerformLambda()
                {
                    var result = string.Empty;

                    result += description.LambdaParameters.Select(param => param.LambdaBodyParameterSetup()).SeparateBySemicolonAndNewLine();

                    if (description.WithStructuralChanges)
                        result +=
                            $@"OriginalLambdaBody({description.LambdaParameters.Select(param => param.StructuralChanges_LambdaBodyParameter())
                                .SeparateByCommaAndNewLine()});";
                    else
                        result +=
                            $@"OriginalLambdaBody({description.LambdaParameters.Select(param => param.LambdaBodyParameter())
                                .SeparateByCommaAndNewLine()});";

                    return result;
                }

                string ExecuteMethodForJob() =>
                    $@"
                    public void Execute()
                    {{
                        {PerformLambda()}
                    }}";

                /*
                string ExecuteMethodDefault() => $@"
                    public void Execute(Unity.Entities.ArchetypeChunk chunk, int batchIndex)
                    {{
                        {GetChunkNativeArrays(description)}

                        int count = chunk.Count;
                        for (int entityIndex = 0; entityIndex != count; entityIndex++)
                        {{
                            {PerformLambda()}
                        }}
                    }}";
                */

                string ExecuteMethodDefault() => $@"
                        [global::System.Runtime.CompilerServices.CompilerGenerated]
                        public void Execute(in global::Unity.Entities.ArchetypeChunk chunk, int batchIndex, bool useEnabledMask, in global::Unity.Burst.Intrinsics.v128 chunkEnabledMask)
                        {{
                            {GetChunkNativeArrays(description)}
                            int chunkEntityCount = chunk.Count;
                            {"int matchingEntityCount = 0;".EmitIfTrue(description.NeedsEntityInQueryIndex)}
                            if (!useEnabledMask)
                            {{
                                for(var entityIndex = 0; entityIndex < chunkEntityCount; ++entityIndex)
                                {{
                                    {"var entityInQueryIndex = __ChunkBaseEntityIndices[batchIndex] + matchingEntityCount++;".EmitIfTrue(description.NeedsEntityInQueryIndex)}
                                    {PerformLambda()}
                                }}
                            }}
                            else
                            {{
                                int edgeCount = global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong0 ^ (chunkEnabledMask.ULong0 << 1)) +
                                                global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong1 ^ (chunkEnabledMask.ULong1 << 1)) - 1;
                                bool useRanges = edgeCount <= 4;
                                if (useRanges)
                                {{
                                    int entityIndex = 0;
                                    int batchEndIndex = 0;
                                    while (global::Unity.Entities.EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out entityIndex, out batchEndIndex))
                                    {{
                                        while (entityIndex < batchEndIndex)
                                        {{
                                            {"var entityInQueryIndex = __ChunkBaseEntityIndices[batchIndex] + matchingEntityCount++;".EmitIfTrue(description.NeedsEntityInQueryIndex)}
                                            {PerformLambda()}
                                            entityIndex++;
                                        }}
                                    }}
                                }}
                                else
                                {{
                                    ulong mask64 = chunkEnabledMask.ULong0;
                                    int count = global::Unity.Mathematics.math.min(64, chunkEntityCount);
                                    for (var entityIndex = 0; entityIndex < count; ++entityIndex)
                                    {{
                                        if ((mask64 & 1) != 0)
                                        {{
                                            {"var entityInQueryIndex = __ChunkBaseEntityIndices[batchIndex] + matchingEntityCount++;".EmitIfTrue(description.NeedsEntityInQueryIndex)}
                                            {PerformLambda()}
                                        }}
                                        mask64 >>= 1;
                                    }}
                                    mask64 = chunkEnabledMask.ULong1;
                                    for (var entityIndex = 64; entityIndex < chunkEntityCount; ++entityIndex)
                                    {{
                                        if ((mask64 & 1) != 0)
                                        {{
                                            {"var entityInQueryIndex = __ChunkBaseEntityIndices[batchIndex] + matchingEntityCount++;".EmitIfTrue(description.NeedsEntityInQueryIndex)}
                                            {PerformLambda()}
                                        }}
                                        mask64 >>= 1;
                                    }}
                                }}
                           }}
                    }}";

                /*
                string ExecuteMethodWithEntityInQueryIndex() =>
                    $@"
                    public void Execute(in Unity.Entities.ArchetypeChunk chunk,
                        int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
                    public void Execute(Unity.Entities.ArchetypeChunk chunk, int batchIndex, int indexOfFirstEntityInQuery)
                    {{
                        {GetChunkNativeArrays(description)}

                        int count = chunk.Count;
                        for (int entityIndex = 0; entityIndex != count; entityIndex++)
                        {{
                            int entityInQueryIndex = indexOfFirstEntityInQuery + entityIndex;
                            {PerformLambda()}
                        }}
                    }}";
                    */

                string ExecuteMethodForStructuralChanges() =>
                    $@"
                    public void RunWithStructuralChange(global::Unity.Entities.EntityQuery query)
                    {{
                        {TypeCreationHelpers.GeneratedLineTriviaToGeneratedSource}
                        var mask = query.GetEntityQueryMask();
                        global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeCreateGatherEntitiesResult(ref query, out var gatherEntitiesResult);
                        {StructuralChanges_GetTypeIndices(description)}

                        try
                        {{
                            int entityCount = gatherEntitiesResult.EntityCount;
                            for (int entityIndex = 0; entityIndex != entityCount; entityIndex++)
                            {{
                                var entity = global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetEntityFromGatheredEntities(ref gatherEntitiesResult, entityIndex);
                                if (mask.MatchesIgnoreFilter(entity))
                                {{
                                    {StructuralChanges_ReadLambdaParams(description)}
                                    {UpdateComponentLookupAtInnerIteration(description)}
                                    {PerformLambda()}
                                    {StructuralChanges_WriteBackLambdaParams(description)}
                                }}
                            }}
                        }}
                        finally
                        {{
                            global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeReleaseGatheredEntities(ref query, ref gatherEntitiesResult);
                        }}
                    }}";

                string ExecuteMethodForStructuralChangesWithEntities() =>
                    $@"
                    public void RunWithStructuralChange(global::Unity.Entities.EntityQuery query, global::Unity.Collections.NativeArray<Entity> withEntities)
                    {{
                        {TypeCreationHelpers.GeneratedLineTriviaToGeneratedSource}
                        var mask = query.GetEntityQueryMask();
                        {StructuralChanges_GetTypeIndices(description)}

                        int entityCount = withEntities.Length;
                        for (int entityIndex = 0; entityIndex != entityCount; entityIndex++)
                        {{
                            var entity = withEntities[entityIndex];
                            if (mask.MatchesIgnoreFilter(entity))
                            {{
                                {StructuralChanges_ReadLambdaParams(description)}
                                {UpdateComponentLookupAtInnerIteration(description)}
                                {PerformLambda()}
                                {StructuralChanges_WriteBackLambdaParams(description)}
                            }}
                        }}
                    }}";

                string ExecuteMethod()
                {
                    if (description.LambdaJobKind == LambdaJobKind.Job)
                        return ExecuteMethodForJob();
                    if (description.WithStructuralChanges)
                        return ExecuteMethodForStructuralChanges();
                    if (description.WithStructuralChanges && description.WithFilterEntityArray != null)
                        return ExecuteMethodForStructuralChangesWithEntities();
                    return ExecuteMethodDefault();
                }

                string DisposeOnCompletionMethod()
                {
                    if (!description.DisposeOnJobCompletionVariables.Any())
                        return string.Empty;

                    var allDisposableFieldsAndChildren = new List<string>();
                    foreach (var variable in description.DisposeOnJobCompletionVariables)
                        allDisposableFieldsAndChildren.AddRange(
                            variable.NamesOfAllDisposableMembersIncludingOurselves());

                    return description.Schedule.Mode switch
                    {
                        ScheduleMode.Run =>
                            $@"
                            public void DisposeOnCompletion()
				            {{
                                {allDisposableFieldsAndChildren.Select(disposable => $"{disposable}.Dispose();").SeparateByNewLine()}
                            }}",

                        _ => $@"
                            public global::Unity.Jobs.JobHandle DisposeOnCompletion(global::Unity.Jobs.JobHandle jobHandle)
				            {{
                                {allDisposableFieldsAndChildren.Select(disposable => $"jobHandle = {disposable}.Dispose(jobHandle);").SeparateByNewLine()}
                                return jobHandle;
                            }}"
                    };
                }

                string JobInterface()
                {
                    var jobInterface = "";
                    if (description.LambdaJobKind == LambdaJobKind.Job)
                        jobInterface = " : global::Unity.Jobs.IJob";
                    else if (!description.WithStructuralChanges)
                        jobInterface = " : global::Unity.Entities.IJobChunk";

                    if (!string.IsNullOrEmpty(jobInterface) && description.IsForDOTSRuntime)
                        jobInterface += ", global::Unity.Jobs.IJobBase";

                    return jobInterface;
                }

                static string RunWithoutJobSystemMethod(LambdaJobDescription description)
                {
                    switch (description.LambdaJobKind)
                    {
                        case LambdaJobKind.Entities:
                        {
                            var jobInterfaceType = "global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface";

                            return
                                description.WithFilterEntityArray != null
                                    ? $@"
                                    {Common.BurstCompileAttribute(description)}
                                    {Common.MonoPInvokeCallbackAttributeAttribute(description)}
                                    public static void RunWithoutJobSystem(ref global::Unity.Entities.EntityQuery query, global::System.IntPtr limitToEntityArrayPtr, int limitToEntityArrayLength, global::System.IntPtr jobPtr)
                                    {{
                                        {EnterTempMemoryScope(description)}
                                        try
                                        {{
                                            {jobInterfaceType}.RunWithoutJobsInternal(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<{description.JobStructName}>(jobPtr), ref query, limitToEntityArrayPtr, limitToEntityArrayLength);
                                        }}
                                        finally
                                        {{
                                            {ExitTempMemoryScope(description)}
                                        }}
                                    }}"
                                    : $@"
                                    {Common.BurstCompileAttribute(description)}
                                    {Common.MonoPInvokeCallbackAttributeAttribute(description)}
                                    public static void RunWithoutJobSystem(ref global::Unity.Entities.EntityQuery query, global::System.IntPtr jobPtr)
                                    {{
                                        {EnterTempMemoryScope(description)}
                                        try
                                        {{
                                            {jobInterfaceType}.RunWithoutJobsInternal(ref global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<{description.JobStructName}>(jobPtr), ref query);
                                        }}
                                        finally
                                        {{
                                            {ExitTempMemoryScope(description)}
                                        }}
                                    }}";
                        }

                        default:
                            return
                                $@"
                                {Common.BurstCompileAttribute(description)}
                                {Common.MonoPInvokeCallbackAttributeAttribute(description)}
                                public static void RunWithoutJobSystem(global::System.IntPtr jobPtr)
                                {{
                                    {EnterTempMemoryScope(description)}
                                    try
                                    {{
                                        global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeAsRef<{description.JobStructName}>(jobPtr).Execute();
                                    }}
                                    finally
                                    {{
                                        {ExitTempMemoryScope(description)}
                                    }}
                                }}";
                    }
                }

                static string EnterTempMemoryScope(LambdaJobDescription description)
                {
                    return !description.IsForDOTSRuntime ? "" : "global::Unity.Runtime.TempMemoryScope.EnterScope();";
                }

                static string ExitTempMemoryScope(LambdaJobDescription description)
                {
                    return !description.IsForDOTSRuntime ? "" : "global::Unity.Runtime.TempMemoryScope.ExitScope();";
                }

                static string RunWithoutJobSystemDelegateFields(LambdaJobDescription description)
                {
                    var delegateName = description.LambdaJobKind switch
                    {
                        LambdaJobKind.Entities when description.WithFilterEntityArray != null => "global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegateLimitEntities",
                        LambdaJobKind.Entities => "global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate",
                        LambdaJobKind.Job => "global::Unity.Entities.Internal.InternalCompilerInterface.JobRunWithoutJobSystemDelegate",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var fieldDeclaration = string.Empty;

                    /* TODO: Once Burst 1.6 lands in Entities, we should be able to just run through .Invoke everywhere as Burst will do IL patching to remove
                     the performance issues around calling .Invoke directly. */
                    fieldDeclaration = $"internal static {delegateName} FunctionPtrFieldNoBurst;";
                    if (description.Burst.IsEnabled)
                        fieldDeclaration += $"internal static {delegateName} FunctionPtrFieldBurst;";

                    return fieldDeclaration;
                }

                static string IJobBaseMethods(LambdaJobDescription description)
                {
                    return @"
                    public void PrepareJobAtExecuteTimeFn_Gen(int jobIndex, global::System.IntPtr localNodes)
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public void CleanupJobAfterExecuteTimeFn_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public void CleanupJobFn_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.ManagedJobForEachDelegate GetExecuteMethod_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public int GetUnmanagedJobSize_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.ManagedJobMarshalDelegate GetMarshalToBurstMethod_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.ManagedJobMarshalDelegate GetMarshalFromBurstMethod_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public int IsBursted_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }" +
                    (description.SafetyChecksEnabled ? @"
                    public int PrepareJobAtPreScheduleTimeFn_Gen(ref global::Unity.Development.JobsDebugger.DependencyValidator data, ref global::Unity.Jobs.JobHandle dependsOn, global::System.IntPtr deferredSafety)
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public void PrepareJobAtPostScheduleTimeFn_Gen(ref global::Unity.Development.JobsDebugger.DependencyValidator data, ref global::Unity.Jobs.JobHandle scheduledJob)
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public void PatchMinMax_Gen(global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.MinMax param)
                    {
                        throw new global::System.NotImplementedException();
                    }

                    public int GetSafetyFieldCount()
                    {
                        throw new global::System.NotImplementedException();
                    }" : "") +
                    (description.SafetyChecksEnabled || description.DOTSRuntimeProfilerEnabled ? @"
                    public int GetJobNameIndex_Gen()
                    {
                        throw new global::System.NotImplementedException();
                    }" : "");
                }

                static string GenerateProfilerMarker(LambdaJobDescription description)
                {
                    if (!description.ProfilerEnabled || description.IsForDOTSRuntime)
                        return "";
                    string marker = "public static readonly global::Unity.Profiling.ProfilerMarker s_ProfilerMarker = new global::Unity.Profiling.ProfilerMarker(";
                    if (description.Schedule.Mode == ScheduleMode.Run)
                    {
                        if (description.Burst.IsEnabled)
                            marker +=
                                "new global::Unity.Profiling.ProfilerCategory(\"Burst\", global::Unity.Profiling.ProfilerCategoryColor.BurstJobs), ";
                        marker += "\"";
                        marker += description.Name;
                        marker += $"\");{Environment.NewLine}";
                    }
                    else
                    {
                        marker += "\"";
                        marker += description.Name;
                        if (description.Schedule.Mode == ScheduleMode.Schedule)
                            marker += ".Schedule";
                        else
                            marker += ".ScheduleParallel";
                        marker += $"\");{Environment.NewLine}";
                    }

                    return marker;
                }
            }

            public static MethodDeclarationSyntax LambdaBodyMethodFor(LambdaJobDescription description)
            {
                var template = $@"{(description.NeedsUnsafe ? "unsafe " : string.Empty)} void {description.LambdaBodyMethodName}({description.LambdaParameters.Select(
                    param => param.LambdaBodyMethodParameter(description.Burst.IsEnabled)).SeparateByComma()})
                {description.RewrittenLambdaBody.ToString()}";
                return (MethodDeclarationSyntax) SyntaxFactory.ParseMemberDeclaration(template);
            }

            public static MethodDeclarationSyntax CreateExecuteMethod(LambdaJobDescription description)
            {
                var temporaryEcbCreation = CreateTemporaryEcb(description);

                bool addJobHandleForProducer = description.EntityCommandBufferParameter is {Playback: {IsImmediate: false}}
                                               && description.EntityCommandBufferParameter.Playback.ScheduleMode != ScheduleMode.Run;

                var emitProfilerMarker = description.ProfilerEnabled && !description.IsForDOTSRuntime;

                var template =
                    $@"{(description.NeedsUnsafe ? "unsafe " : string.Empty)} {ReturnType()} {description.ExecuteInSystemMethodName}({ExecuteMethodParams()})
                    {{
                        {ComponentTypeHandleFieldUpdate()}
                        {(description.WithStructuralChanges ? "" : UpdateComponentLookupAtScheduleSite(description) /* .wsc done per entity instead */)}
                        {temporaryEcbCreation.Code}
                        var __job = new {description.JobStructName}
                        {{
                            {JobStructFieldAssignments().SeparateByCommaAndNewLine()}
                        }};
                        {Common.SharedComponentFilterInvocations(description)}
                        {"global::Unity.Jobs.JobHandle __jobHandle;".EmitIfTrue(description.Schedule.DependencyArgument != null)}
                        {CalculateChunkBaseEntityIndices()}
                        {$"using ({description.JobStructName}.s_ProfilerMarker.Auto()) {{".EmitIfTrue(emitProfilerMarker)}
                        {ScheduleInvocation()}
                        {"}".EmitIfTrue(emitProfilerMarker)}
                        {Common.ResetSharedComponentFilter(description)}
                        {DisposeOnCompletionInvocation()}
                        {$"{description.EntityCommandBufferParameter?.GeneratedEcbFieldNameInSystemBaseType}.AddJobHandleForProducer(Dependency);".EmitIfTrue(addJobHandleForProducer)}
                        {$"{TemporaryJobEntityCommandBufferVariableName}.Playback(EntityManager);".EmitIfTrue(temporaryEcbCreation.Success)}
                        {$"{TemporaryJobEntityCommandBufferVariableName}.Dispose();".EmitIfTrue(temporaryEcbCreation.Success)}
                        {WriteBackCapturedVariablesAssignments()}
                        {"return __jobHandle;".EmitIfTrue(description.Schedule.DependencyArgument != null)}
                    }}";

                string CalculateChunkBaseEntityIndices()
                {
                    if (!description.NeedsEntityInQueryIndex)
                        return string.Empty;

                    if (description.Schedule.Mode == ScheduleMode.Run)
                        return $"__job.__ChunkBaseEntityIndices = {description.EntityQueryFieldName}.CalculateBaseEntityIndexArray(this.CheckedStateRef.WorldUpdateAllocator);";

                    if (description.Schedule.DependencyArgument != null)
                        return @$"
                            global::Unity.Collections.NativeArray<int> {description.ChunkBaseEntityIndexFieldName} = {description.EntityQueryFieldName}.CalculateBaseEntityIndexArrayAsync(this.CheckedStateRef.WorldUpdateAllocator, __inputDependency, out __inputDependency);
                            __job.__ChunkBaseEntityIndices = {description.ChunkBaseEntityIndexFieldName};";
                    return @$"
                            global::Unity.Jobs.JobHandle outHandle;
                            global::Unity.Collections.NativeArray<int> {description.ChunkBaseEntityIndexFieldName} = {description.EntityQueryFieldName}.CalculateBaseEntityIndexArrayAsync(this.CheckedStateRef.WorldUpdateAllocator, Dependency, out outHandle);
                            __job.__ChunkBaseEntityIndices = {description.ChunkBaseEntityIndexFieldName};
                            Dependency = outHandle;";
                }

                return (MethodDeclarationSyntax) SyntaxFactory.ParseMemberDeclaration(template);

                string ExecuteMethodParams()
                {
                    string ParamsForCapturedVariable(LambdaCapturedVariableDescription variable)
                    {
                        return
                            description.Schedule.Mode == ScheduleMode.Run && variable.IsWritable
                                ? $@"ref {variable.Type.ToFullName()} {variable.Symbol.Name}"
                                : $@"{variable.Type.ToFullName()} {variable.Symbol.Name}";
                    }

                    var paramStrings = new List<string>();
                    paramStrings.AddRange(description.VariablesCaptured.Where(variable => !variable.IsThis)
                        .Select(ParamsForCapturedVariable));
                    if (description.Schedule.DependencyArgument != null)
                    {
                        paramStrings.Add(@"global::Unity.Jobs.JobHandle __inputDependency");
                    }

                    if (description.WithFilterEntityArray != null)
                    {
                        paramStrings.Add($@"global::Unity.Collections.NativeArray<Entity> __entityArray");
                    }

                    foreach (var argument in description.AdditionalVariablesCapturedForScheduling)
                        paramStrings.Add($@"{argument.Symbol.GetSymbolType().ToFullName()} {argument.Name}");

                    return paramStrings.Distinct().SeparateByComma();
                }

                IEnumerable<string> JobStructFieldAssignments()
                {
                    foreach (var capturedVariable in description.VariablesCaptured)
                        yield return $@"{capturedVariable.VariableFieldName} = {capturedVariable.OriginalVariableName}";

                    if (!description.WithStructuralChanges)
                        foreach (var param in description.LambdaParameters)
                            yield return $"{param.FieldAssignmentInGeneratedJobChunkType(description)}";

                    foreach (var field in description.AdditionalFields)
                        yield return field.JobStructAssign();

                    if (description.NeedsTimeData)
                        yield return $"__Time = this.CheckedStateRef.WorldUnmanaged.Time";
                }

                string ComponentTypeHandleFieldUpdate() =>
                    description
                        .LambdaParameters
                        .OfType<IParamRequireUpdate>()
                        .Select(c => c.FormatUpdateInvocation(description))
                        .SeparateByNewLine();

                string ScheduleJobInvocation()
                {
                    switch (description.Schedule.Mode)
                    {
                        case ScheduleMode.Run:
                        {
                            if (description.Burst.IsEnabled)
                            {
                                return $@"
                                        this.CheckedStateRef.CompleteDependency();
                                        var __functionPointer = global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled
                                        ? {description.JobStructName}.FunctionPtrFieldBurst
                                        : {description.JobStructName}.FunctionPtrFieldNoBurst;
                                        global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeRunIJob(ref __job, __functionPointer);";
                            }
                            return $@"
                                        this.CheckedStateRef.CompleteDependency();
                                        __job.Execute();";
                        }

                        case ScheduleMode.Schedule:
                        {
                            return
                                description.Schedule.DependencyArgument != null
                                    ? "__jobHandle = global::Unity.Jobs.IJobExtensions.Schedule(__job, __inputDependency);"
                                    : "this.CheckedStateRef.Dependency = global::Unity.Jobs.IJobExtensions.Schedule(__job, this.CheckedStateRef.Dependency);";
                        }
                    }

                    throw new InvalidOperationException(
                        "Can't create ScheduleJobInvocation for invalid lambda description");
                }

                string ScheduleEntitiesInvocation()
                {
                    string EntityQueryParameter() => $"{description.EntityQueryFieldName}";

                    string OutputOptionalEntityArrayParameter(ArgumentSyntax entityArray)
                    {
                        if (entityArray != null) return ", __entityArray";

                        // Special case when we have WithScheduleGranularity but no entity array
                        // the only ScheduleParallel signature available with a granularity parameter we can use here is
                        // ScheduleParallel(job, query, granularity, entity array)
                        // to call it, entity array must be set to "default"
                        return description.WithScheduleGranularityArgumentSyntaxes.Count > 0 ? ", default" : "";
                    }

                    string OutputOptionalGranularityParameter() =>
                        description.WithScheduleGranularityArgumentSyntaxes.Count > 0 ? $", {description.WithScheduleGranularityArgumentSyntaxes.ElementAt(0)}" : "";

                    string OutputOptionalChunkBaseIndexArrayParameter()
                    {
                        // If the job requires entityInQueryIndex, the array of per-chunk base entity indices must be passed as
                        // the last parameter of ScheduleParallel() in order to call JobsUtility.PatchBufferMinMaxRanges()
                        bool needsEntityInQueryIndex = description.NeedsEntityInQueryIndex;
                        bool isScheduleParallel = (description.Schedule.Mode == ScheduleMode.ScheduleParallel);
                        return (needsEntityInQueryIndex && isScheduleParallel) ? $", {description.ChunkBaseEntityIndexFieldName}" : "";
                    }

                    // Certain schedule paths require .Schedule/.Run calls that aren't in the IJobChunk public API,
                    // and only appear in InternalCompilerInterface
                    string JobChunkExtensionType() => "global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface";

                    switch (description.Schedule.Mode)
                    {
                        case ScheduleMode.Run:
                        {
                            if (description.WithStructuralChanges)
                            {
                                var entityArray =
                                    ", __entityArray".EmitIfTrue(description.WithFilterEntityArray != null);

                                return $@"
                                if(!{description.EntityQueryFieldName}.IsEmptyIgnoreFilter)
                                {{
                                    this.CheckedStateRef.CompleteDependency();
                                    __job.RunWithStructuralChange({description.EntityQueryFieldName}{entityArray});
                                }}";
                            }

                            if (description.Burst.IsEnabled)
                            {
                                var scheduleMethod = "UnsafeRunJobChunk";

                                var additionalSetup = @$"var __functionPointer = global::Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobCompilerEnabled
                                    ? {description.JobStructName}.FunctionPtrFieldBurst
                                    : {description.JobStructName}.FunctionPtrFieldNoBurst;";
                                var scheduleArguments = (description.WithFilterEntityArray != null)
                                    ? $"ref __job, {description.EntityQueryFieldName}, global::Unity.Entities.Internal.InternalCompilerInterface.UnsafeGetEntityArrayIntPtr(__entityArray), __entityArray.Length, __functionPointer"
                                    : $"ref __job, {description.EntityQueryFieldName}, __functionPointer";

                                return $@"
                                if(!{description.EntityQueryFieldName}.IsEmptyIgnoreFilter)
                                {{
                                    this.CheckedStateRef.CompleteDependency();
                                    {additionalSetup}
                                    global::Unity.Entities.Internal.InternalCompilerInterface.{scheduleMethod}({scheduleArguments});
                                }}";
                            }

                            return $@"
                                            if(!{description.EntityQueryFieldName}.IsEmptyIgnoreFilter)
                                            {{
                                                this.CheckedStateRef.CompleteDependency();
                                                {JobChunkExtensionType()}.RunByRefWithoutJobs(ref __job, {description.EntityQueryFieldName});
                                            }}";
                        }

                        case ScheduleMode.Schedule:
                        {
                            return
                                description.Schedule.DependencyArgument != null
                                    ? $@"__jobHandle = {JobChunkExtensionType()}.Schedule(__job, {EntityQueryParameter()}{OutputOptionalEntityArrayParameter(description.WithFilterEntityArray)}, __inputDependency);"
                                    : $@"this.CheckedStateRef.Dependency = {JobChunkExtensionType()}.Schedule(__job, {EntityQueryParameter()}{OutputOptionalEntityArrayParameter(description.WithFilterEntityArray)}, this.CheckedStateRef.Dependency);";
                        }

                        case ScheduleMode.ScheduleParallel:
                        {
                            return
                                description.Schedule.DependencyArgument != null
                                    ? $@"__jobHandle = {JobChunkExtensionType()}.ScheduleParallel(__job, {EntityQueryParameter()}{OutputOptionalGranularityParameter()}{OutputOptionalEntityArrayParameter(description.WithFilterEntityArray)}, __inputDependency{OutputOptionalChunkBaseIndexArrayParameter()});"
                                    : $@"this.CheckedStateRef.Dependency = {JobChunkExtensionType()}.ScheduleParallel(__job, {EntityQueryParameter()}{OutputOptionalGranularityParameter()}{OutputOptionalEntityArrayParameter(description.WithFilterEntityArray)}, this.CheckedStateRef.Dependency{OutputOptionalChunkBaseIndexArrayParameter()});";
                        }
                    }

                    throw new InvalidOperationException("Can't create ScheduleJobInvocation for invalid lambda description");
                }

                string ScheduleInvocation()
                {
                    return description.LambdaJobKind == LambdaJobKind.Entities
                        ? ScheduleEntitiesInvocation()
                        : ScheduleJobInvocation();
                }

                string WriteBackCapturedVariablesAssignments()
                {
                    if (description.Schedule.Mode != ScheduleMode.Run)
                        return string.Empty;

                    return
                        description
                            .VariablesCaptured
                            .Where(variable => !variable.IsThis && variable.IsWritable)
                            .Select(variable => $@"{variable.OriginalVariableName} = __job.{variable.VariableFieldName};")
                            .SeparateByNewLine();
                }

                string DisposeOnCompletionInvocation()
                {
                    if (!description.DisposeOnJobCompletionVariables.Any())
                        return string.Empty;
                    if (description.Schedule.Mode == ScheduleMode.Run)
                        return @"__job.DisposeOnCompletion();";
                    if (description.Schedule.DependencyArgument != null)
                        return @"__jobHandle = __job.DisposeOnCompletion(__jobHandle);";
                    return $@"this.CheckedStateRef.Dependency = __job.DisposeOnCompletion(this.CheckedStateRef.Dependency);";
                }

                string ReturnType() =>
                    description.Schedule.DependencyArgument != null ? "global::Unity.Jobs.JobHandle" : "void";
            }

            static string UpdateComponentLookupAtScheduleSite(LambdaJobDescription description) =>
                description.AdditionalFields
                    .Select(c => c.FormatUpdateInvocation())
                    .SeparateByNewLine();

            static string UpdateComponentLookupAtInnerIteration(LambdaJobDescription description) =>
                description.AdditionalFields
                    .Select(c => $"{c.FieldName}.Update(__this);")
                    .SeparateByNewLine();

            static (bool Success, string Code) CreateTemporaryEcb(LambdaJobDescription description)
            {
                if (description.EntityCommandBufferParameter == null)
                    return (false, string.Empty);
                return
                    description.EntityCommandBufferParameter.Playback.IsImmediate
                    ? (true, $"global::Unity.Entities.EntityCommandBuffer {TemporaryJobEntityCommandBufferVariableName} = new global::Unity.Entities.EntityCommandBuffer(this.World.UpdateAllocator.ToAllocator);")
                        : (false, string.Empty);
            }
        }
    }
}
