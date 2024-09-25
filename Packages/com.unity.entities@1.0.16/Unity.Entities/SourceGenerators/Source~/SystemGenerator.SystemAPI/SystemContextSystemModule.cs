using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI
{
    public partial class SystemContextSystemModule : ISystemModule {
        readonly Dictionary<TypeDeclarationSyntax, List<CandidateSyntax>> m_Candidates = new Dictionary<TypeDeclarationSyntax, List<CandidateSyntax>>();
        public IEnumerable<(SyntaxNode SyntaxNode, TypeDeclarationSyntax SystemType)> Candidates {
            get {
                foreach (var type in m_Candidates) {
                    foreach (var candidate in type.Value) {
                        yield return (candidate.Node, type.Key);
                    }
                }
            }
        }
        public bool RequiresReferenceToBurst => false;

        public void OnReceiveSyntaxNode(SyntaxNode node) {
            switch (node) {
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: { } nameSyntax } } invocation: // makes sure to use SystemAPI.*** as the reference instead of *** as the SystemAPI part should also dissapear
                    InvocationWithNameOrMember(invocation, nameSyntax);
                    break;
                case InvocationExpressionSyntax { Expression: SimpleNameSyntax nameSyntax } invocation:
                    InvocationWithNameOrMember(invocation, nameSyntax);
                    break;
                case IdentifierNameSyntax nameSyntax:
                    PropertyWithNameOrMember(nameSyntax);
                    break;
            }

            void PropertyWithNameOrMember(SimpleNameSyntax nameSyntax) {
                switch (nameSyntax.Identifier.ValueText) {
                    case "Time":
                        var systemTypeSyntax = nameSyntax.AncestorOfKindOrDefault<TypeDeclarationSyntax>();
                        if (systemTypeSyntax != null)
                            m_Candidates.Add(systemTypeSyntax, new CandidateSyntax(CandidateType.TimeData, CandidateFlags.None, nameSyntax));
                        break;
                }
            }

            void InvocationWithNameOrMember(InvocationExpressionSyntax invocation, SimpleNameSyntax nodeContainedByInvocation) {
                switch (nodeContainedByInvocation.Identifier.ValueText) {
                    // Component
                    case "GetComponentLookup":
                        AddCandidate(CandidateFlags.None, CandidateType.GetComponentLookup);
                        break;
                    case "GetComponent":
                        AddCandidate(CandidateFlags.None, CandidateType.GetComponent);
                        break;
                    case "GetComponentRO":
                        AddCandidate(CandidateFlags.None, CandidateType.GetComponentRO);
                        break;
                    case "GetComponentRW":
                        AddCandidate(CandidateFlags.None, CandidateType.GetComponentRW);
                        break;
                    case "SetComponent":
                        AddCandidate(CandidateFlags.None, CandidateType.SetComponent);
                        break;
                    case "HasComponent":
                        AddCandidate(CandidateFlags.None, CandidateType.HasComponent);
                        break;
                    case "IsComponentEnabled":
                        AddCandidate(CandidateFlags.None, CandidateType.IsComponentEnabled);
                        break;
                    case "SetComponentEnabled":
                        AddCandidate(CandidateFlags.None, CandidateType.SetComponentEnabled);
                        break;

                    // Buffer
                    case "GetBufferLookup":
                        AddCandidate(CandidateFlags.None, CandidateType.GetBufferLookup);
                        break;
                    case "GetBuffer":
                        AddCandidate(CandidateFlags.None, CandidateType.GetBuffer);
                        break;
                    case "HasBuffer":
                        AddCandidate(CandidateFlags.None, CandidateType.HasBuffer);
                        break;
                    case "IsBufferEnabled":
                        AddCandidate(CandidateFlags.None, CandidateType.IsBufferEnabled);
                        break;
                    case "SetBufferEnabled":
                        AddCandidate(CandidateFlags.None, CandidateType.SetBufferEnabled);
                        break;

                    // StorageInfo/Exists
                    case "GetEntityStorageInfoLookup":
                        AddCandidate(CandidateFlags.None, CandidateType.GetEntityStorageInfoLookup);
                        break;
                    case "Exists":
                        AddCandidate(CandidateFlags.None, CandidateType.Exists);
                        break;

                    // Singleton
                    case "GetSingleton":
                        AddCandidate(CandidateFlags.ReadOnly, CandidateType.Singleton);
                        break;
                    case "GetSingletonEntity":
                        AddCandidate(CandidateFlags.ReadOnly | CandidateFlags.NoGenericGeneration, CandidateType.Singleton);
                        break;
                    case "SetSingleton":
                        AddCandidate(CandidateFlags.None, CandidateType.Singleton);
                        break;
                    case "GetSingletonRW":
                        AddCandidate(CandidateFlags.None, CandidateType.Singleton);
                        break;
                    case "TryGetSingletonRW":
                        AddCandidate(CandidateFlags.None, CandidateType.Singleton);
                        break;
                    case "TryGetSingletonBuffer":
                        AddCandidate(CandidateFlags.None, CandidateType.Singleton);
                        break;
                    case "TryGetSingletonEntity":
                        AddCandidate(CandidateFlags.ReadOnly, CandidateType.Singleton);
                        break;
                    case "GetSingletonBuffer":
                        AddCandidate(CandidateFlags.None, CandidateType.Singleton);
                        break;
                    case "TryGetSingleton":
                        AddCandidate(CandidateFlags.ReadOnly, CandidateType.Singleton);
                        break;
                    case "HasSingleton":
                        AddCandidate(CandidateFlags.ReadOnly, CandidateType.Singleton);
                        break;

                    // Aspect
                    case "GetAspect":
                        AddCandidate(CandidateFlags.None, CandidateType.Aspect);
                        break;

                    // TypeHandle
                    case "GetEntityTypeHandle":
                        AddCandidate(CandidateFlags.None, CandidateType.EntityTypeHandle);
                        break;
                    case "GetComponentTypeHandle":
                        AddCandidate(CandidateFlags.None, CandidateType.ComponentTypeHandle);
                        break;
                    case "GetBufferTypeHandle":
                        AddCandidate(CandidateFlags.None, CandidateType.BufferTypeHandle);
                        break;
                    case "GetSharedComponentTypeHandle":
                        AddCandidate(CandidateFlags.None, CandidateType.SharedComponentTypeHandle);
                        break;

                    void AddCandidate(CandidateFlags flags, CandidateType type) {
                        m_Candidates.Add(
                            nodeContainedByInvocation.AncestorOfKind<TypeDeclarationSyntax>(),
                            new CandidateSyntax(type, flags, invocation));
                    }
                }
            }
        }

        public bool RegisterChangesInSystem(SystemDescription desc) {
            if (!m_Candidates.ContainsKey(desc.SystemTypeSyntax)) return false;
            var candidatesInSystem = m_Candidates[desc.SystemTypeSyntax];
            desc.Rewriters.Add(new SystemApiSystemReplacer<SystemDescription>(candidatesInSystem.AsReadOnly(), desc.HandlesDescription, desc));
            return true;
        }
    }
}
