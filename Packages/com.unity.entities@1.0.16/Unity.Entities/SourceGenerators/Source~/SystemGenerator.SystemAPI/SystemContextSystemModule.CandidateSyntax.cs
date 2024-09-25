using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI
{
    public partial class SystemContextSystemModule
    {
        public readonly struct CandidateSyntax : ISystemCandidate {
            public CandidateSyntax(CandidateType type, CandidateFlags flags, SyntaxNode node) {
                Type = type;
                Flags = flags;
                Node = node;
            }

            public string CandidateTypeName => $"SystemAPI.{Type.ToString()}";
            public SyntaxNode Node { get; }
            public readonly CandidateType Type;
            public readonly CandidateFlags Flags;

            public int GetOriginalLineNumber() => Node.GetLineNumber();

            public static SyntaxNode GetFieldExpression(SyntaxNode newestNode)
                => newestNode.Parent is MemberAccessExpressionSyntax { Expression: { } expression }
                    && (expression is IdentifierNameSyntax { Identifier: { ValueText: "SystemAPI" } } || expression is ThisExpressionSyntax)
                        ? newestNode.Parent : newestNode;

            public InvocationExpressionSyntax GetInvocationExpression(SyntaxNode newestNode) => newestNode as InvocationExpressionSyntax;

            public static SimpleNameSyntax GetSimpleName(SyntaxNode newestNode) {
                if (!(newestNode is InvocationExpressionSyntax invocation)) return newestNode as SimpleNameSyntax;
                return invocation.Expression switch {
                    MemberAccessExpressionSyntax member => member.Name,
                    SimpleNameSyntax sn => sn,
                    _ => null
                };
            }
        }
    }

    public enum CandidateType {
        TimeData,
        GetComponentLookup,
        GetComponent,
        GetComponentRO,
        GetComponentRW,
        SetComponent,
        HasComponent,
        IsComponentEnabled,
        SetComponentEnabled,
        Singleton,
        GetBufferLookup,
        GetBuffer,
        HasBuffer,
        IsBufferEnabled,
        SetBufferEnabled,
        GetEntityStorageInfoLookup,
        Exists,
        Aspect,
        ComponentTypeHandle,
        BufferTypeHandle,
        SharedComponentTypeHandle,
        EntityTypeHandle
    }

    [Flags]
    public enum CandidateFlags {
        None = 0,
        ReadOnly = 1,
        NoGenericGeneration = 2,
        All = int.MaxValue
    }
}
