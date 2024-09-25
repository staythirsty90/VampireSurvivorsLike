#nullable enable
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen.JobEntity
{
    public partial class JobEntityModule
    {
        struct KnownJobEntityInfo : ISystemCandidate
        {
            JobEntityCandidate m_Candidate;
            TypeInfo m_TypeInfo;
            bool m_IsExtensionMethodUsed;

            public static bool TryCreate(ref SystemDescription systemDescription, JobEntityCandidate candidate, out KnownJobEntityInfo result)
            {
                result = new KnownJobEntityInfo
                {
                    m_Candidate = candidate
                };

                // Checks if the Candidate is JobEntity and Get Type info
                ExpressionSyntax? jobEntityInstance = candidate.MemberAccessExpressionSyntax.Expression as IdentifierNameSyntax;
                jobEntityInstance ??= candidate.MemberAccessExpressionSyntax.Expression as ObjectCreationExpressionSyntax;

                if (jobEntityInstance == null)
                    return false;

                result.m_TypeInfo = systemDescription.SemanticModel.GetTypeInfo(jobEntityInstance);

                bool isJobEntity;
                (isJobEntity, result.m_IsExtensionMethodUsed) = DoesTypeInheritJobEntityAndUseExtensionMethod(result.m_TypeInfo);

                return isJobEntity;
            }

            static (bool IsKnownCandidate, bool IsExtensionMethodUsed) DoesTypeInheritJobEntityAndUseExtensionMethod(TypeInfo typeInfo) =>
                typeInfo.Type.InheritsFromInterface("Unity.Entities.IJobEntity")
                    ? (IsKnownCandidate: true, IsExtensionMethodUsed: false) // IsExtensionMethodUsed is ignored if IsCandidate is false, so we don't need to test the same thing twice
                    : (IsKnownCandidate: typeInfo.Type.ToFullName() == "global::Unity.Entities.IJobEntityExtensions", IsExtensionMethodUsed: true);

            /// <returns> Replaced Invocation Scheduling Expression, null if invalid </returns>
            public ExpressionSyntax? GetAndAddScheduleExpression(ref SystemDescription systemDescription, int i, MemberAccessExpressionSyntax currentSchedulingNode)
            {
                // Get Job Info
                var jobArgumentUsedInSchedulingMethod = currentSchedulingNode.Expression;
                var jobEntityType = (INamedTypeSymbol)m_TypeInfo.Type!;

                // Update Job Info if Extension method is used
                if (m_IsExtensionMethodUsed)
                {
                    // Silent fail, compiler will give correct CSC error on our behalf.
                    if (!(currentSchedulingNode.Parent is InvocationExpressionSyntax invocationExpression))
                        return null;

                    // Find JobData Syntax Passed To ExtensionMethod
                    var jobDataArgumentIndex = -1;
                    for (var argumentIndex = 0; argumentIndex < invocationExpression.ArgumentList.Arguments.Count; argumentIndex++)
                    {
                        var argument = invocationExpression.ArgumentList.Arguments[argumentIndex];
                        if (argument.NameColon == null)
                            jobDataArgumentIndex = argumentIndex;
                        else if (argument.NameColon.Name.Identifier.ValueText == "jobData")
                            jobDataArgumentIndex = argumentIndex;
                    }
                    if (jobDataArgumentIndex == -1)
                        return null;
                    jobArgumentUsedInSchedulingMethod = invocationExpression.ArgumentList.Arguments[jobDataArgumentIndex].Expression;

                    // Get JobEntity Symbol Passed To ExtensionMethod - Using Candidate for Semantic Reference
                    jobEntityType = m_Candidate.Invocation!.ArgumentList.Arguments[jobDataArgumentIndex].Expression switch
                    {
                        ObjectCreationExpressionSyntax objectCreationExpressionSyntax
                            when systemDescription.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax).Symbol is IMethodSymbol
                            {
                                ReceiverType: INamedTypeSymbol namedTypeSymbol
                            } => namedTypeSymbol,

                        IdentifierNameSyntax identifierNameSyntax
                            when systemDescription.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol is ILocalSymbol
                            {
                                Type: INamedTypeSymbol namedTypeSymbol
                            } => namedTypeSymbol,
                        _ => null
                    };
                }

                // Get Additional info
                var scheduleMode = ScheduleModeHelpers.GetScheduleModeFromNameOfMemberAccess(m_Candidate.MemberAccessExpressionSyntax);
                if (!systemDescription.TryGetSystemStateParameterName(m_Candidate, out var systemStateExpression))
                    return null;

                var (userDefinedQuery, userDefinedDependency) =
                    GetUserDefinedQueryAndDependency(
                        ref systemDescription,
                        m_IsExtensionMethodUsed,
                        (currentSchedulingNode.Parent as InvocationExpressionSyntax)!,
                        m_Candidate.Invocation!,
                        scheduleMode);

                // Get or create `__TypeHandle.MyJob__JobEntityHandle`
                var jobEntityHandle = systemDescription.HandlesDescription.GetOrCreateJobEntityHandle(jobEntityType, userDefinedQuery == null);
                var jobEntityHandleExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("__TypeHandle"), SyntaxFactory.IdentifierName(jobEntityHandle));

                // is DynamicQuery ? `userQuery` : `__TypeHandle.MyJob__JobEntityHandle.DefaultQuery`
                var entityQuery = userDefinedQuery ?? SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, jobEntityHandleExpression, SyntaxFactory.IdentifierName("DefaultQuery"));
                var scheduleMethodName = AddSchedulingMethodToSystem(ref systemDescription, scheduleMode, i, jobEntityType.ToFullName(), jobEntityHandle);

                return new SchedulingExpressionCreateInfo(
                    scheduleMethodName,
                    scheduleMode,
                    hasUserDefinedQuery: userDefinedQuery != null,
                    jobArgumentUsedInSchedulingMethod,
                    entityQuery,
                    userDefinedDependency,
                    systemStateExpression
                ).ToExpressionSyntax();
            }

            enum ArgumentType
            {
                EntityQuery,
                Dependency,
                JobEntity
            }

            static (ExpressionSyntax? UserDefinedEntityQuery, ExpressionSyntax? UserDefinedDependency)
                GetUserDefinedQueryAndDependency(ref SystemDescription context, bool isExtensionMethodUsed, InvocationExpressionSyntax giveBackInvocation, InvocationExpressionSyntax semanticOriginalInvocation, ScheduleMode scheduleMode)
            {
                ExpressionSyntax? entityQueryArgument = null;
                ExpressionSyntax? dependencyArgument = null;

                for (var i = 0; i < semanticOriginalInvocation.ArgumentList.Arguments.Count; i++)
                {
                    var semanticOriginalArgument = semanticOriginalInvocation.ArgumentList.Arguments[i];
                    var giveBackArgument = giveBackInvocation.ArgumentList.Arguments[i];

                    var type = semanticOriginalArgument.NameColon == null
                        ? ParseUnnamedArgument(ref context, i)
                        : ParseNamedArgument();

                    ArgumentType ParseUnnamedArgument(ref SystemDescription context, int argumentPosition)
                    {
                        switch (argumentPosition+(isExtensionMethodUsed?0:1))
                        {
                            case 0:
                                return ArgumentType.JobEntity;
                            case 1: // Could be EntityQuery or dependsOn
                            {
                                if (semanticOriginalArgument.Expression is DefaultExpressionSyntax {Type: var defaultType})
                                    return defaultType.ToString().Contains("JobHandle") ? ArgumentType.Dependency : ArgumentType.EntityQuery;

                                return context.SemanticModel.GetTypeInfo(semanticOriginalArgument.Expression).Type.ToFullName() == "global::Unity.Entities.EntityQuery"
                                    ? ArgumentType.EntityQuery : ArgumentType.Dependency;
                            }
                            case 2: // dependsOn
                                return ArgumentType.Dependency;
                        }

                        throw new ArgumentOutOfRangeException();
                    }

                    ArgumentType ParseNamedArgument()
                        => semanticOriginalArgument.NameColon.Name.Identifier.ValueText switch
                        {
                            "query" => ArgumentType.EntityQuery,
                            "dependsOn" => ArgumentType.Dependency,
                            "jobData" => ArgumentType.JobEntity,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                    switch (type)
                    {
                        case ArgumentType.EntityQuery:
                            entityQueryArgument = giveBackArgument.Expression;
                            continue;
                        case ArgumentType.Dependency:
                            dependencyArgument = giveBackArgument.Expression;
                            continue;
                    }
                }

                return (entityQueryArgument, dependencyArgument);
            }

            static string AddSchedulingMethodToSystem(ref SystemDescription systemDescription, ScheduleMode scheduleMode, int methodId, string fullTypeName, string jobEntityHandle)
            {
                var containsReturn = !scheduleMode.IsRun();
                var methodName = $"__ScheduleViaJobChunkExtension_{methodId}";
                var methodWithArguments = scheduleMode.GetScheduleMethodWithArguments();
                var returnType = containsReturn ? "global::Unity.Jobs.JobHandle" : "void";
                var returnExpression = $"{"return ".EmitIfTrue(containsReturn)}__TypeHandle.{jobEntityHandle}.{methodWithArguments}";
                var refType = "ref ".EmitIfTrue(scheduleMode.IsByRef());

                var method =
                    $@"[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    {returnType} {methodName}({refType}{fullTypeName} job, global::Unity.Entities.EntityQuery query, global::Unity.Jobs.JobHandle dependency, ref global::Unity.Entities.SystemState state, bool hasUserDefinedQuery)
                    {{
                        {fullTypeName}.InternalCompiler.CheckForErrors({scheduleMode.GetScheduleTypeAsNumber()});

                        {PossiblyThrowUserDefinedQueryException(fullTypeName, checkQuery: systemDescription.IsUnityCollectionChecksEnabled || systemDescription.IsDotsDebugMode)}

                        {"dependency = ".EmitIfTrue(containsReturn)}__TypeHandle.{jobEntityHandle}.UpdateBaseEntityIndexArray(ref job, query, {"dependency, ".EmitIfTrue(containsReturn)}ref state);
                        __TypeHandle.{jobEntityHandle}.AssignEntityManager(ref job, state.EntityManager);
                        __TypeHandle.{jobEntityHandle}.__TypeHandle.Update(ref state);
                        {returnExpression};
                    }}";

                systemDescription.NewMiscellaneousMembers.Add(SyntaxFactory.ParseMemberDeclaration(method));
                return methodName;
            }

            static string PossiblyThrowUserDefinedQueryException(string jobTypeFullName, bool checkQuery)
            {
                if (!checkQuery)
                    return string.Empty;

                return
                    $@"if (Unity.Burst.CompilerServices.Hint.Unlikely(hasUserDefinedQuery))
                    {{
                        int requiredComponentCount = {jobTypeFullName}.InternalCompilerQueryAndHandleData.GetRequiredComponentTypeCount();
                        global::System.Span<Unity.Entities.ComponentType> requiredComponentTypes = stackalloc Unity.Entities.ComponentType[requiredComponentCount];

                        {jobTypeFullName}.InternalCompilerQueryAndHandleData.AddRequiredComponentTypes(ref requiredComponentTypes);

                        if (!{jobTypeFullName}.InternalCompilerQueryAndHandleData.QueryHasRequiredComponentsForExecuteMethodToRun(ref query, ref requiredComponentTypes))
                        {{
                            throw new global::System.InvalidOperationException(
                                ""When scheduling an instance of `{jobTypeFullName}` with a custom query, the query must (at the very minimum) contain all the components required for `{jobTypeFullName}.Execute()` to run."");
                        }}
                    }}";
            }

            public string CandidateTypeName => m_Candidate.CandidateTypeName;
            public SyntaxNode Node => m_Candidate.Node;
        }

        readonly ref struct SchedulingExpressionCreateInfo
        {
            readonly string m_SchedulingMethodName;
            readonly ScheduleMode m_ScheduleMode;
            readonly ExpressionSyntax m_Job;
            readonly ExpressionSyntax m_QueryToUse;
            readonly ExpressionSyntax? m_UserDefinedDependency;
            readonly ExpressionSyntax m_SystemStateExpression;
            readonly bool m_HasUserDefinedQuery;

            public SchedulingExpressionCreateInfo(
                string schedulingMethodName,
                ScheduleMode scheduleMode,
                bool hasUserDefinedQuery,
                ExpressionSyntax job,
                ExpressionSyntax queryToUse,
                ExpressionSyntax? userDefinedDependency,
                ExpressionSyntax systemStateExpression)
            {
                m_SchedulingMethodName = schedulingMethodName;
                m_ScheduleMode = scheduleMode;
                m_Job = job;
                m_HasUserDefinedQuery = hasUserDefinedQuery;
                m_QueryToUse = queryToUse;
                m_UserDefinedDependency = userDefinedDependency;
                m_SystemStateExpression = systemStateExpression;
            }

            /// <summary>
            /// Creates ExpressionSyntax for scheduling, e.g. `someSchedulingMethod(ref SomeJob, someQuery, someDependency, ref systemState)`
            /// </summary>
            /// <returns>The created ExpressionSyntax</returns>
            public ExpressionSyntax ToExpressionSyntax()
            {
                // Dependency: `systemState.Dependency`
                var dependencyNode = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, m_SystemStateExpression, SyntaxFactory.IdentifierName("Dependency"));

                // Arguments: `ref SomeJob, someQuery, someDependency, ref systemState`
                var argumentList = SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(null, m_ScheduleMode.IsByRef() ? SyntaxFactory.Token(SyntaxKind.RefKeyword) : SyntaxFactory.Token(SyntaxKind.None), m_Job.WithoutLeadingTrivia()),
                    SyntaxFactory.Argument(m_QueryToUse),
                    SyntaxFactory.Argument(m_UserDefinedDependency ?? dependencyNode),
                    SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), m_SystemStateExpression),
                    SyntaxFactory.Argument(m_HasUserDefinedQuery ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression) : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                });

                // Finalize invocation: `someSchedulingMethod(ref SomeJob, someQuery, someDependency, ref systemState)`
                ExpressionSyntax replaceToNode =
                    SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(m_SchedulingMethodName), SyntaxFactory.ArgumentList(argumentList));

                // Maybe prefix assignment: `systemState.Dependency = someSchedulingMethod(ref SomeJob, someQuery, someDependency, ref systemState)`
                var doStateDepsAssignment = m_UserDefinedDependency == null;
                doStateDepsAssignment &= !m_ScheduleMode.IsRun();
                if (doStateDepsAssignment)
                    replaceToNode = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, dependencyNode, replaceToNode);

                // Return with trivia from job - important when its called as extension method
                return replaceToNode.WithLeadingTrivia(m_Job.GetLeadingTrivia());
            }
        }
    }
}
