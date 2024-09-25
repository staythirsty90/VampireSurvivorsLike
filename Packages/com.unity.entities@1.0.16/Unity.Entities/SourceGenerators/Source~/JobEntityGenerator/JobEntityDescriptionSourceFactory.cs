using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public partial class JobEntityDescription
    {
        public string Generate()
        {
            var hasEnableableComponent = true; // HasEnableableComponent();
                                               // Currently, it is never set to false.
                                               // It could be false in cases where the IJobEntity is constructing its own EntityQuery based on the provided parameters,
                                               // in which case it can statically determine at source-generation time whether the query contains any enableable components
                                               // (and thus whether it needs to generate the extra code to handle enabled bits correctly).
                                               // This work has not yet been implemented. (e.g. when enableablebits is turned of with static query generation.)
                                               // Check should be based on whether the query contains enableablebits, not if the parameters does (cause some chunks might still need to check if they are enabled)
                                               // Discussion: https://github.cds.internal.unity3d.com/unity/dots/pull/3217#discussion_r227389
                                               // Also do this for EntitiesSourceFactory.JobStructFor if that is still a thing.

            var inheritsFromBeginEndChunk = m_JobEntityTypeSymbol.InheritsFromInterface("Unity.Entities.IJobEntityChunkBeginEnd");
            string partialStructImplementation =
                $@"[global::System.Runtime.CompilerServices.CompilerGenerated]
partial struct {TypeName} : global::Unity.Entities.IJobChunk
{{
    InternalCompilerQueryAndHandleData.TypeHandle __TypeHandle;
    {EntityManager()}
    {ChunkBaseEntityIndices()}

    [global::System.Runtime.CompilerServices.CompilerGenerated]
    public void Execute(in global::Unity.Entities.ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in global::Unity.Burst.Intrinsics.v128 chunkEnabledMask)
    {{
        {$"var shouldExecuteChunk = OnChunkBegin(in chunk, chunkIndexInQuery, useEnabledMask, in chunkEnabledMask);{Environment.NewLine}        if (shouldExecuteChunk){{".EmitIfTrue(inheritsFromBeginEndChunk)}
        {string.Join(Environment.NewLine + "        ", UserExecuteMethodParams.Select(p => p.VariableDeclarationAtStartOfExecuteMethod).Distinct())}
        int chunkEntityCount = chunk.Count;
        int matchingEntityCount = 0;
        {@"if (!useEnabledMask)
        {".EmitIfTrue(hasEnableableComponent)}
            for(int entityIndexInChunk = 0; entityIndexInChunk < chunkEntityCount; ++entityIndexInChunk)
            {{
                {string.Join(Environment.NewLine + "                ", UserExecuteMethodParams.Where(p => p.RequiresExecuteMethodArgumentSetup).Select(p => p.ExecuteMethodArgumentSetup))}
                Execute({UserExecuteMethodParams.Select(param => param.ExecuteMethodArgumentValue).SeparateByComma()});
                matchingEntityCount++;
            }}
        {$@"}}
        else
        {{
            int edgeCount = global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong0 ^ (chunkEnabledMask.ULong0 << 1)) +
                            global::Unity.Mathematics.math.countbits(chunkEnabledMask.ULong1 ^ (chunkEnabledMask.ULong1 << 1)) - 1;
            bool useRanges = edgeCount <= 4;
            if (useRanges)
            {{
                int entityIndexInChunk = 0;
                int chunkEndIndex = 0;
                while (global::Unity.Entities.EnabledBitUtility.TryGetNextRange(chunkEnabledMask, chunkEndIndex, out entityIndexInChunk, out chunkEndIndex))
                {{
                    while (entityIndexInChunk < chunkEndIndex)
                    {{
                        {string.Join(Environment.NewLine + "                        ", UserExecuteMethodParams.Where(p => p.RequiresExecuteMethodArgumentSetup).Select(p => p.ExecuteMethodArgumentSetup))}
                        Execute({UserExecuteMethodParams.Select(param => param.ExecuteMethodArgumentValue).SeparateByComma()});
                        entityIndexInChunk++;
                        matchingEntityCount++;
                    }}
                }}
            }}
            else
            {{
                ulong mask64 = chunkEnabledMask.ULong0;
                int count = global::Unity.Mathematics.math.min(64, chunkEntityCount);
                for (int entityIndexInChunk = 0; entityIndexInChunk < count; ++entityIndexInChunk)
                {{
                    if ((mask64 & 1) != 0)
                    {{
                        {string.Join(Environment.NewLine + "                        ", UserExecuteMethodParams.Where(p => p.RequiresExecuteMethodArgumentSetup).Select(p => p.ExecuteMethodArgumentSetup))}
                        Execute({UserExecuteMethodParams.Select(param => param.ExecuteMethodArgumentValue).SeparateByComma()});
                        matchingEntityCount++;
                    }}
                    mask64 >>= 1;
                }}
                mask64 = chunkEnabledMask.ULong1;
                for (int entityIndexInChunk = 64; entityIndexInChunk < chunkEntityCount; ++entityIndexInChunk)
                {{
                    if ((mask64 & 1) != 0)
                    {{
                        {string.Join(Environment.NewLine + "                        ", UserExecuteMethodParams.Where(p => p.RequiresExecuteMethodArgumentSetup).Select(p => p.ExecuteMethodArgumentSetup))}
                        Execute({UserExecuteMethodParams.Select(param => param.ExecuteMethodArgumentValue).SeparateByComma()});
                        matchingEntityCount++;
                    }}
                    mask64 >>= 1;
                }}
            }}
        }}".EmitIfTrue(hasEnableableComponent)}
        {"} OnChunkEnd(in chunk, chunkIndexInQuery, useEnabledMask, in chunkEnabledMask, shouldExecuteChunk);".EmitIfTrue(inheritsFromBeginEndChunk)}
    }}
    {GetScheduleAndRunMethods()}

    {CreateTypeHandleSyntax(m_CheckUserDefinedQueryForScheduling)}

    /// <summary> Internal structure used by the compiler </summary>
    {MakeInternalCompilerStructure()}
}}";

            return partialStructImplementation.ReplaceLineEndings();
        }

        string CreateTypeHandleSyntax(bool checkQuery = false)
        {
            const string jobExtensions = "global::Unity.Entities.JobChunkExtensions";
            const string internalJobExtensions = "global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkInterface";

            var additionalSyntax =
                @$"{MethodsForGettingRequiredComponents(checkQuery)}

                public void Init(ref global::Unity.Entities.SystemState state, bool assignDefaultQuery)
                {{
                    if (assignDefaultQuery) {{
                        __AssignQueries(ref state);
                    }}
                    __TypeHandle.__AssignHandles(ref state);
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public void Run(ref {FullTypeName} job, global::Unity.Entities.EntityQuery query)
                {{
                    job.__TypeHandle = __TypeHandle;
                    {(HasManagedComponents ? internalJobExtensions : jobExtensions)}.{(HasManagedComponents ? "RunByRefWithoutJobs(ref job, query)" : "RunByRef(ref job, query)")};
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public global::Unity.Jobs.JobHandle Schedule(ref {FullTypeName} job, global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependency)
                {{
                    job.__TypeHandle = __TypeHandle;
                    return {jobExtensions}.ScheduleByRef(ref job, query, dependency);
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public global::Unity.Jobs.JobHandle ScheduleParallel(ref {FullTypeName} job, global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependency)
                {{
                    job.__TypeHandle = __TypeHandle;
                    return {(m_HasEntityIndexInQuery ? internalJobExtensions : jobExtensions)}.ScheduleParallelByRef(ref job, query, dependency{(m_HasEntityIndexInQuery?", job.__ChunkBaseEntityIndices":"")});
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public void UpdateBaseEntityIndexArray(ref {FullTypeName} job, global::Unity.Entities.EntityQuery query, ref global::Unity.Entities.SystemState state)
                {{
                    {(m_HasEntityIndexInQuery ? @"var baseEntityIndexArray = query.CalculateBaseEntityIndexArray(state.WorldUpdateAllocator);
                    job.__ChunkBaseEntityIndices = baseEntityIndexArray;" : "")}
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public global::Unity.Jobs.JobHandle UpdateBaseEntityIndexArray(ref {FullTypeName} job, global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependency, ref global::Unity.Entities.SystemState state)
                {{
                    {(m_HasEntityIndexInQuery ? @"var baseEntityIndexArray = query.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, dependency, out var indexDependency);
                    job.__ChunkBaseEntityIndices = baseEntityIndexArray;
                    return indexDependency;" : "return dependency;")}
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public void AssignEntityManager(ref {FullTypeName} job, global::Unity.Entities.EntityManager entityManager)
                {{
                    {(m_RequiresEntityManager ? @"job.__EntityManager = entityManager;" : "")}
                }}
            ";

            return HandlesDescription.GetTypeHandleForInitialPartial(true, true, additionalSyntax, m_HandlesDescription);
        }

        private string MethodsForGettingRequiredComponents(bool checkQuery)
        {
            if (!checkQuery)
                return string.Empty;

            return
                $@"[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public static int GetRequiredComponentTypeCount()
                    => {m_ComponentTypesInExecuteMethod.Count}{m_AspectTypesInExecuteMethod.Select(a => $" + {a.TypeSymbol.ToFullName()}.GetRequiredComponentTypeCount()").SeparateBy("")};

                public static void AddRequiredComponentTypes(ref global::System.Span<Unity.Entities.ComponentType> components)
                {{
                    {m_ComponentTypesInExecuteMethod.Select((c, i) => $"components[{i}] = {c.ToString()};").SeparateByNewLine()}
                    {AddAspectComponents(m_AspectTypesInExecuteMethod).SeparateByNewLine()}
                }}

                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                public static bool QueryHasRequiredComponentsForExecuteMethodToRun(ref EntityQuery userDefinedQuery, ref global::System.Span<global::Unity.Entities.ComponentType> components)
                    => global::Unity.Entities.Internal.InternalCompilerInterface.EntityQueryInterface.HasComponentsRequiredForExecuteMethodToRun(ref userDefinedQuery, ref components);";
        }

        private IEnumerable<string> AddAspectComponents(List<ParameterTypeInJobEntityExecuteMethod> aspectsUsedInJobEntity)
        {
            if (aspectsUsedInJobEntity.Count == 0)
                yield break;

            yield return $"int startAddIndex = {m_ComponentTypesInExecuteMethod.Count};";
            for (var index = 0; index < aspectsUsedInJobEntity.Count; index++)
            {
                var aspect = aspectsUsedInJobEntity[index];

                yield return $"int aspect{index}ComponentTypeCount = {aspect.TypeSymbol.ToFullName()}.GetRequiredComponentTypeCount();";
                yield return $"global::System.Span<global::Unity.Entities.ComponentType> aspect{index}Components = stackalloc global::Unity.Entities.ComponentType[aspect{index}ComponentTypeCount];";
                yield return $"{aspect.TypeSymbol.ToFullName()}.AddRequiredComponentTypes(ref aspect{index}Components);";
                yield return $@"for (int i = 0; i < aspect{index}ComponentTypeCount; i++)
                                {{
                                    components[startAddIndex + i] = aspect{index}Components[i];
                                }}";
                if (index < aspectsUsedInJobEntity.Count - 1)
                    yield return $"startAddIndex += aspect{index}ComponentTypeCount;";
            }
        }

        string MakeInternalCompilerStructure() => @$"public struct InternalCompiler
    {{
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        [global::System.Diagnostics.Conditional(""ENABLE_UNITY_COLLECTIONS_CHECKS"")]
        // scheduleType 0:Run, 1:Schedule, 2:ScheduleParallel
        public static void CheckForErrors(int scheduleType) {{
            {@"if(scheduleType == 2) throw new global::System.InvalidOperationException(""Tried to ScheduleParallel a job with a managed execute signature. Please use .Run or .Schedule instead."");".EmitIfTrue(m_RequiresEntityManager)}
        }}
    }}";

        string EntityManager() => "public global::Unity.Entities.EntityManager __EntityManager;".EmitIfTrue(m_RequiresEntityManager);

        string ChunkBaseEntityIndices() => "[global::Unity.Collections.ReadOnly] public global::Unity.Collections.NativeArray<int> __ChunkBaseEntityIndices;".EmitIfTrue(m_HasEntityIndexInQuery);

        string GetScheduleAndRunMethods()
        {
            const string source = @"global::Unity.Jobs.JobHandle __ThrowCodeGenException() => throw new global::System.Exception(""This method should have been replaced by source gen."");

    // Emitted to disambiguate scheduling method invocations
    public void Run() => __ThrowCodeGenException();
    public void RunByRef() => __ThrowCodeGenException();
    public void Run(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();
    public void RunByRef(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();

    public global::Unity.Jobs.JobHandle Schedule(global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleByRef(global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle Schedule(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleByRef(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public void Schedule() => __ThrowCodeGenException();
    public void ScheduleByRef() => __ThrowCodeGenException();
    public void Schedule(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();
    public void ScheduleByRef(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();

    public global::Unity.Jobs.JobHandle ScheduleParallel(global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleParallelByRef(global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleParallel(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleParallelByRef(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleParallel(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn, global::Unity.Collections.NativeArray<int> chunkBaseEntityIndices) => __ThrowCodeGenException();
    public global::Unity.Jobs.JobHandle ScheduleParallelByRef(global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependsOn, global::Unity.Collections.NativeArray<int> chunkBaseEntityIndices) => __ThrowCodeGenException();
    public void ScheduleParallel() => __ThrowCodeGenException();
    public void ScheduleParallelByRef() => __ThrowCodeGenException();
    public void ScheduleParallel(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();
    public void ScheduleParallelByRef(global::Unity.Entities.EntityQuery query) => __ThrowCodeGenException();";

            return source;
        }
    }
}
