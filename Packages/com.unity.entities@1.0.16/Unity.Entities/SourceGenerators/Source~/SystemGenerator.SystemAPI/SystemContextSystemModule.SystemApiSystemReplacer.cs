using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGenerator.Common;
using static Unity.Entities.SourceGen.SystemGenerator.Common.TypeHandleFieldDescription;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI
{
    public partial class SystemContextSystemModule
    {
        class SystemApiSystemReplacer<TAdditionalHandlesInfo> : SystemRewriter where TAdditionalHandlesInfo : IAdditionalHandlesInfo, ISourceGeneratorDiagnosable
        {
            HandlesDescription m_HandlesDescription;
            TAdditionalHandlesInfo m_AdditionalHandlesInfo;

            ReadOnlyCollection<CandidateSyntax> m_Candidates;

            public override IEnumerable<SyntaxNode> NodesToTrack => m_Candidates.Select(c=>c.Node);

            StatementHashStack m_StatementHashStack;
            public SystemApiSystemReplacer(ReadOnlyCollection<CandidateSyntax> candidates, HandlesDescription handlesDescription, TAdditionalHandlesInfo additionalHandlesInfo) {
                m_Candidates = candidates;
                m_HandlesDescription = handlesDescription;
                m_AdditionalHandlesInfo = additionalHandlesInfo;
                m_StatementHashStack = StatementHashStack.CreateInstance();
            }

            Dictionary<SyntaxNode, CandidateSyntax> m_SyntaxToCandidate;
            public override SyntaxNode VisitTrackedSystem(SyntaxNode systemRootNode, string originalFilePath)
            {
                m_OriginalFilePath = originalFilePath;

                m_SyntaxToCandidate = new Dictionary<SyntaxNode, CandidateSyntax>(m_Candidates.Count);
                foreach (var candidate in m_Candidates)
                {
                    var newNode = systemRootNode.GetCurrentNodes(candidate.Node).FirstOrDefault() ?? candidate.Node;
                    m_SyntaxToCandidate.Add(newNode, candidate);
                }

                return Visit(systemRootNode);
            }

            // These need to be saved as Visiting can ascend from recursion until we find the node we actually want to replace
            bool m_HasChangedMember;
            SyntaxNode m_NodeToReplace;
            SyntaxNode m_ReplacementNode;
            int m_OriginalLineNumber;

            // Actual statement insertion and replacement occurs here
            public override SyntaxNode Visit(SyntaxNode nodeIn)
            {
                if (nodeIn == null)
                    return null;

                // Visit children (and allow replacements to occur there)
                var replacedNodeAndChildren = base.Visit(nodeIn);

                // Perform replacement of candidates
                if (m_SyntaxToCandidate.TryGetValue(nodeIn, out var candidate)) {
                    var (original, replacement) = TryGetReplacementNodeFromCandidate(candidate, replacedNodeAndChildren, nodeIn);
                    if (replacement != null)
                    {
                        m_ReplacementNode = replacement;
                        m_NodeToReplace = original;
                        m_OriginalLineNumber = candidate.GetOriginalLineNumber();
                    }
                }

                if (replacedNodeAndChildren == m_NodeToReplace) {
                    replacedNodeAndChildren = m_ReplacementNode;
                    m_HasChangedMember = true;
                }

                // Insert statement prolog before replacement
                if (replacedNodeAndChildren is StatementSyntax statement && nodeIn == m_StatementHashStack.ActiveStatement) {
                    var poppedStatement = m_StatementHashStack.PopSyntax();
                    poppedStatement.Add(statement.WithHiddenLineTrivia() as StatementSyntax);
                    poppedStatement[0] = poppedStatement[0].WithLineTrivia(m_OriginalFilePath, m_OriginalLineNumber) as StatementSyntax;

                    replacedNodeAndChildren = SyntaxFactory.Block(new SyntaxList<AttributeListSyntax>(),
                        SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                        new SyntaxList<StatementSyntax>(poppedStatement),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));
                }

                if (replacedNodeAndChildren is MemberDeclarationSyntax memberSyntax && m_HasChangedMember) {
                    RecordChangedMember(memberSyntax);
                    m_HasChangedMember = false;
                }

                return replacedNodeAndChildren;
            }

            (SyntaxNode original, SyntaxNode replacement) TryGetReplacementNodeFromCandidate(CandidateSyntax candidate, SyntaxNode newestNode, SyntaxNode newestNonReplaced) {
                var semanticModel = m_AdditionalHandlesInfo.SemanticModel;

                var resolveCandidateSymbol = semanticModel.GetSymbolInfo(candidate.Node);
                var nodeSymbol = resolveCandidateSymbol.Symbol ?? resolveCandidateSymbol.CandidateSymbols.FirstOrDefault();
                var parentTypeInfo = nodeSymbol?.ContainingType;

                var fullName = parentTypeInfo?.ToFullName();
                var isSystemApi = fullName == "global::Unity.Entities.SystemAPI";
                var isManagedApi = fullName == "global::Unity.Entities.SystemAPI.ManagedAPI";
                if (!(isSystemApi || isManagedApi || (candidate.Type == CandidateType.Singleton && parentTypeInfo.Is("Unity.Entities.ComponentSystemBase"))))
                    return (null,null);

                switch (candidate.Type) {
                    case CandidateType.TimeData: {
                        return m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)
                            ? (CandidateSyntax.GetFieldExpression(newestNode), SyntaxFactory.ParseExpression($"{systemStateExpression}.WorldUnmanaged.Time"))
                            : (null, null);
                    }
                }

                // No type argument (EntityStorageInfoLookup, Exists)
                if (nodeSymbol is IMethodSymbol { TypeArguments: { Length: 0 } }) {
                    var invocationExpression = candidate.GetInvocationExpression(newestNode);
                    switch (candidate.Type) {
                        case CandidateType.GetEntityStorageInfoLookup:
                        case CandidateType.Exists:
                        {
                            var storageInfoLookup = m_HandlesDescription.GetOrCreateEntityStorageInfoLookupField();
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{storageInfoLookup}.Update(ref {systemStateExpression});");

                            return candidate.Type switch
                            {
                                CandidateType.GetEntityStorageInfoLookup => (invocationExpression, ExpressionSyntaxWithTypeHandle(storageInfoLookup)),
                                CandidateType.Exists => (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(storageInfoLookup), "Exists", invocationExpression.ArgumentList)),
                                _ => throw new ArgumentOutOfRangeException() // Shouldn't be hit as outer switch is checking that only correct candidates go in!
                            };
                        }

                        case CandidateType.EntityTypeHandle:
                        {
                            var typeHandleField = m_HandlesDescription.GetOrCreateEntityTypeHandleField();
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);
                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{typeHandleField}.Update(ref {systemStateExpression});");
                            return (invocationExpression, ExpressionSyntaxWithTypeHandle(typeHandleField));
                        }
                    }

                }

                // Based on type argument
                else if (nodeSymbol is IMethodSymbol { TypeArguments: { Length: 1 } } namedTypeSymbolWithTypeArg) {
                    var typeArgument = namedTypeSymbolWithTypeArg.TypeArguments.First();
                    var invocationExpression = candidate.GetInvocationExpression(newestNode);
                    if (TryGetSystemBaseGeneric(out var replacer))
                        return replacer;

                    switch (candidate.Type) {
                        // Component
                        case CandidateType.GetComponentLookup: {
                            var @readonly = false;
                            var args = invocationExpression.ArgumentList.Arguments.ToArray();
                            if (args.Length == 0 || bool.TryParse(args[0].Expression.ToString(), out @readonly)) {
                                var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, @readonly);
                                if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                    return (null, null);

                                var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                                m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                                return (invocationExpression, ExpressionSyntaxWithTypeHandle(lookup));
                            }

                            var methodDeclarationSyntax = candidate.Node.AncestorOfKind<MethodDeclarationSyntax>();
                            if (methodDeclarationSyntax.Identifier.ValueText == "OnCreate") {
                                var containingMethodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                                if (containingMethodSymbol.Parameters.Length == 0 ||
                                    (containingMethodSymbol.Parameters.Length == 1 && containingMethodSymbol.Parameters[0].Type.Is("Unity.Entities.SystemState")))
                                {
                                    m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression); // Ok to not handle as you can't be in OnCreate without it. By definition of above SystemState constraint.
                                    var sn = CandidateSyntax.GetSimpleName(newestNode);
                                    return (invocationExpression,
                                        SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{systemStateExpression}.{sn}"), invocationExpression.ArgumentList));
                                }
                            }

                            SystemAPIErrors.SGSA0002(m_AdditionalHandlesInfo, candidate);

                            break;
                        }
                        case CandidateType.GetComponent when isManagedApi: {
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);
                            return (invocationExpression, InvocationExpression($"{systemStateExpression}.EntityManager","GetComponentObject",
                                candidate.Node.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList, invocationExpression.ArgumentList));
                        }
                        case CandidateType.GetComponent: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, true);
                            var entitySnippet = invocationExpression.ArgumentList.Arguments.First().Expression;
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, ElementAccessExpression(ExpressionSyntaxWithTypeHandle(lookup), entitySnippet));
                        }
                        case CandidateType.GetComponentRO: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, true);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(lookup), "GetRefRO", invocationExpression.ArgumentList));
                        }
                        case CandidateType.GetComponentRW: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, false);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRW<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(lookup), "GetRefRW", invocationExpression.ArgumentList));
                        }
                        case CandidateType.SetComponent: {
                            var args = invocationExpression.ArgumentList.Arguments.ToArray();
                            if (args.Length != 2) return (null, null);
                            var (entityArg, componentArg) = args[0].NameColon?.Name.Identifier.ValueText == "component" ? (args[1], args[0]) : (args[0], args[1]);
                            typeArgument = typeArgument.TypeKind == TypeKind.TypeParameter
                                ? semanticModel.GetTypeInfo(componentArg.Expression).Type
                                : typeArgument;

                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, false);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRW<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, ElementAccessExpression(ExpressionSyntaxWithTypeHandle(lookup), entityArg.Expression), componentArg.Expression));
                        }
                        case CandidateType.HasComponent when isManagedApi: {
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);
                            return (invocationExpression, InvocationExpression($"{systemStateExpression}.EntityManager","HasComponent",
                                candidate.Node.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList, invocationExpression.ArgumentList));
                        }
                        case CandidateType.HasComponent: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, true);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(lookup), "HasComponent", invocationExpression.ArgumentList));
                        }
                        case CandidateType.IsComponentEnabled when isManagedApi: {
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);
                            return (invocationExpression, InvocationExpression($"{systemStateExpression}.EntityManager","IsComponentEnabled",
                                candidate.Node.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList, invocationExpression.ArgumentList));
                        }
                        case CandidateType.IsComponentEnabled: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, true);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(lookup), "IsComponentEnabled", invocationExpression.ArgumentList));
                        }
                        case CandidateType.SetComponentEnabled when isManagedApi: {
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);
                            return (invocationExpression, InvocationExpression($"{systemStateExpression}.EntityManager","SetComponentEnabled",
                                candidate.Node.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList, invocationExpression.ArgumentList));
                        }
                        case CandidateType.SetComponentEnabled: {
                            var lookup = m_HandlesDescription.GetOrCreateComponentLookupField(typeArgument, false);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{lookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement, $"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRW<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(lookup), "SetComponentEnabled", invocationExpression.ArgumentList));
                        }

                        // Buffer
                        case CandidateType.GetBufferLookup: {
                            var @readonly = false;
                            var args = invocationExpression.ArgumentList.Arguments.ToArray();
                            if (args.Length == 0 || bool.TryParse(args[0].Expression.ToString(), out @readonly)) {
                                var bufferLookup = m_HandlesDescription.GetOrCreateBufferLookupField(typeArgument, @readonly);
                                if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                                var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                                m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{bufferLookup}.Update(ref {systemStateExpression});");
                                return (invocationExpression, ExpressionSyntaxWithTypeHandle(bufferLookup));
                            }

                            var methodDeclarationSyntax = candidate.Node.AncestorOfKind<MethodDeclarationSyntax>();
                            if (methodDeclarationSyntax.Identifier.ValueText == "OnCreate") {
                                var containingMethodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                                if (containingMethodSymbol.Parameters.Length == 0 ||
                                    (containingMethodSymbol.Parameters.Length == 1 && containingMethodSymbol.Parameters[0].Type.Is("Unity.Entities.SystemState")))
                                {
                                    m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression);
                                    var sn = CandidateSyntax.GetSimpleName(newestNode);
                                    return (invocationExpression, SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{systemStateExpression}.{sn}"),
                                        invocationExpression.ArgumentList));
                                }
                            }

                            SystemAPIErrors.SGSA0002(m_AdditionalHandlesInfo, candidate);

                            break;
                        }
                        case CandidateType.GetBuffer: {
                            var bufferLookup = m_HandlesDescription.GetOrCreateBufferLookupField(typeArgument, false);
                            var entitySnippet = invocationExpression.ArgumentList.Arguments.First().Expression;
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{bufferLookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement,$"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRW<{typeArgument.ToFullName()}>();"); // Todo in next PR, change SysApi signature to match GetBuffer of EntityManager
                            return (invocationExpression, ElementAccessExpression(ExpressionSyntaxWithTypeHandle(bufferLookup), entitySnippet));
                        }
                        case CandidateType.HasBuffer: {
                            var bufferLookup = m_HandlesDescription.GetOrCreateBufferLookupField(typeArgument, true);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{bufferLookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement,$"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(bufferLookup), "HasBuffer", invocationExpression.ArgumentList));
                        }
                        case CandidateType.IsBufferEnabled: {
                            var bufferLookup = m_HandlesDescription.GetOrCreateBufferLookupField(typeArgument, true);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{bufferLookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement,$"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRO<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(bufferLookup), "IsBufferEnabled", invocationExpression.ArgumentList));
                        }
                        case CandidateType.SetBufferEnabled: {
                            var bufferLookup = m_HandlesDescription.GetOrCreateBufferLookupField(typeArgument, false);
                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{bufferLookup}.Update(ref {systemStateExpression});");
                            m_StatementHashStack.PushStatement(statement,$"{systemStateExpression}.EntityManager.CompleteDependencyBeforeRW<{typeArgument.ToFullName()}>();");
                            return (invocationExpression, InvocationExpression(ExpressionSyntaxWithTypeHandle(bufferLookup), "SetBufferEnabled", invocationExpression.ArgumentList));
                        }

                        // Singleton
                        case CandidateType.Singleton: {
                            var queryFieldName = m_HandlesDescription.GetOrCreateQueryField(
                                new SingleArchetypeQueryFieldDescription(
                                    new Archetype(
                                        new[]
                                        {
                                            new Query
                                            {
                                                IsReadOnly = (candidate.Flags & CandidateFlags.ReadOnly) == CandidateFlags.ReadOnly,
                                                Type = QueryType.All,
                                                TypeSymbol = typeArgument
                                            }
                                        },
                                        Array.Empty<Query>(),
                                        Array.Empty<Query>(),
                                        Array.Empty<Query>(),
                                        Array.Empty<Query>(),
                                        EntityQueryOptions.Default | EntityQueryOptions.IncludeSystems)
                                ));

                            var sn = CandidateSyntax.GetSimpleName(newestNode);
                            var noGenericGeneration = (candidate.Flags & CandidateFlags.NoGenericGeneration) == CandidateFlags.NoGenericGeneration;
                            var memberAccess = noGenericGeneration ? sn.Identifier.ValueText : sn.ToString(); // e.g. GetSingletonEntity<T> -> query.GetSingletonEntity (with no generic)

                            return (invocationExpression, InvocationExpression($"{queryFieldName}", memberAccess, invocationExpression.ArgumentList));
                        }

                        // Aspect
                        case CandidateType.Aspect: {
                            var @readonly = (candidate.Flags == CandidateFlags.ReadOnly);

                            if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression))
                                return (null, null);

                            var entitySnippet = invocationExpression.ArgumentList.Arguments.First();
                            var aspectLookup = m_HandlesDescription.GetOrCreateAspectLookup(typeArgument, @readonly);

                            var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                            m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{aspectLookup}.Update(ref {systemStateExpression});");
                            var completeDependencyStatement = @readonly switch
                                {
                                    true => $"{typeArgument.ToFullName()}.CompleteDependencyBeforeRO(ref {systemStateExpression});",
                                    false => $"{typeArgument.ToFullName()}.CompleteDependencyBeforeRW(ref {systemStateExpression});"
                                };
                            m_StatementHashStack.PushStatement(statement, completeDependencyStatement);

                            var replacementElementAccessExpression = SyntaxFactory.ElementAccessExpression(ExpressionSyntaxWithTypeHandle(aspectLookup))
                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(entitySnippet)));

                            return (invocationExpression, replacementElementAccessExpression);
                        }

                        case CandidateType.BufferTypeHandle:
                        case CandidateType.SharedComponentTypeHandle:
                        case CandidateType.ComponentTypeHandle: {
                            var @readonly = false;
                            var args = invocationExpression.ArgumentList.Arguments.ToArray();
                            if (args.Length == 0 || bool.TryParse(args[0].Expression.ToString(), out @readonly))
                            {
                                var forcedTypeHandleSource = candidate.Type switch
                                {
                                    CandidateType.ComponentTypeHandle => TypeHandleSource.Component,
                                    CandidateType.BufferTypeHandle => TypeHandleSource.BufferElement,
                                    _ => TypeHandleSource.SharedComponent
                                };

                                var typeHandleField = m_HandlesDescription.GetOrCreateTypeHandleField(typeArgument, @readonly, forcedTypeHandleSource);
                                if (!m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression)) return (null, null);

                                var statement = newestNonReplaced.AncestorOfKind<StatementSyntax>();
                                m_StatementHashStack.PushStatement(statement, $"__TypeHandle.{typeHandleField}.Update(ref {systemStateExpression});");
                                return (invocationExpression, ExpressionSyntaxWithTypeHandle(typeHandleField));
                            }

                            var methodDeclarationSyntax = candidate.Node.AncestorOfKind<MethodDeclarationSyntax>();
                            if (methodDeclarationSyntax.Identifier.ValueText == "OnCreate") {
                                var containingMethodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                                if (containingMethodSymbol.Parameters.Length == 0 ||
                                    (containingMethodSymbol.Parameters.Length == 1 && containingMethodSymbol.Parameters[0].Type.Is("Unity.Entities.SystemState")))
                                {
                                    m_AdditionalHandlesInfo.TryGetSystemStateParameterName(candidate, out var systemStateExpression);
                                    var sn = CandidateSyntax.GetSimpleName(newestNode);
                                    return (invocationExpression, SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{systemStateExpression}.{sn}"),
                                        invocationExpression.ArgumentList));
                                }
                            }

                            SystemAPIErrors.SGSA0002(m_AdditionalHandlesInfo, candidate);

                            break;
                        }
                    }

                    // If using a generic that takes part of a method, then it should default to a `InternalCompilerInterface.DontUseThisGetSingleQuery<T>(this).` reference in SystemBase, and cause a compile error in ISystem.
                    bool TryGetSystemBaseGeneric(out (SyntaxNode original, SyntaxNode replacement) replacer)
                    {
                        replacer = (null, null);

                        var usesUnknownTypeArgument = typeArgument is ITypeParameterSymbol;

                        var usingUnkownTypeArgumentIsValid = false;
                        var containingTypeTypeList = m_AdditionalHandlesInfo.TypeSyntax.TypeParameterList; // Can support parents but better restrictive now.
                        if (containingTypeTypeList != null)
                        {
                            var validConstraints = containingTypeTypeList.Parameters;
                            foreach (var validConstraint in validConstraints)
                                usingUnkownTypeArgumentIsValid |= validConstraint.Identifier.ValueText == typeArgument.Name;
                        }

                        if (usesUnknownTypeArgument && !usingUnkownTypeArgumentIsValid) {
                            if (m_AdditionalHandlesInfo.SystemType==SystemType.ISystem)
                                SystemAPIErrors.SGSA0001(m_AdditionalHandlesInfo, candidate);
                            else if (isSystemApi && candidate.Type == CandidateType.Singleton) // Enabled you to use type parameters for SystemAPI singletons inside SystemBase
                            {
                                var sn = CandidateSyntax.GetSimpleName(newestNode);
                                var noGenericGenerationGeneric = (candidate.Flags & CandidateFlags.NoGenericGeneration) == CandidateFlags.NoGenericGeneration;
                                var memberAccessGeneric = noGenericGenerationGeneric ? sn.Identifier.ValueText : sn.ToString(); // e.g. GetSingletonEntity<T> -> query.GetSingletonEntity (with no generic)

                                var dontUseThisGetSingleQueryInvocation = InvocationExpression(
                                    "Unity.Entities.Internal.InternalCompilerInterface",
                                    "OnlyAllowedInSourceGeneratedCodeGetSingleQuery",
                                    TypeArgumentListSyntax(typeArgument.ToFullName()),
                                    ArgumentListSyntax(SyntaxFactory.ThisExpression()));
                                replacer = (invocationExpression, InvocationExpression(dontUseThisGetSingleQueryInvocation, memberAccessGeneric, invocationExpression.ArgumentList));
                            }
                            return true;
                        }

                        return false;
                    }
                }

                return (null, null);
            }

            static ArgumentListSyntax ArgumentListSyntax(ExpressionSyntax expression)
                => SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(expression) }));
            static TypeArgumentListSyntax TypeArgumentListSyntax(string typeName)
                => SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(new[] { (TypeSyntax)SyntaxFactory.IdentifierName(typeName)}));
            static InvocationExpressionSyntax InvocationExpression(ExpressionSyntax from, string invocation, ArgumentListSyntax args)
                => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, from, SyntaxFactory.IdentifierName(invocation)), args);
            static InvocationExpressionSyntax InvocationExpression(string from, string invocation, TypeArgumentListSyntax typeArgs, ArgumentListSyntax args)
                => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(@from), SyntaxFactory.GenericName(SyntaxFactory.Identifier(invocation), typeArgs)), args);
            static InvocationExpressionSyntax InvocationExpression(string from, string invocation, ArgumentListSyntax args)
                => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(@from), SyntaxFactory.IdentifierName(invocation)), args);
            static ElementAccessExpressionSyntax ElementAccessExpression(ExpressionSyntax componentLookup, ExpressionSyntax entitySnippet)
                => SyntaxFactory.ElementAccessExpression(componentLookup, SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(new []{SyntaxFactory.Argument(entitySnippet)})));
            static ExpressionSyntax ExpressionSyntaxWithTypeHandle(string fieldName)
                => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("__TypeHandle"), SyntaxFactory.IdentifierName(fieldName));
        }
    }
}
