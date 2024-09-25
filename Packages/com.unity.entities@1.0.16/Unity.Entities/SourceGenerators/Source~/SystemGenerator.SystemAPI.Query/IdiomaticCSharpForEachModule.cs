using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.SystemGenerator.Common;
using static Unity.Entities.SourceGen.Common.QueryVerification;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI.Query
{
    public partial class IdiomaticCSharpForEachModule : ISystemModule
    {
        private readonly List<QueryCandidate> _queryCandidates = new List<QueryCandidate>();
        private Dictionary<TypeDeclarationSyntax, QueryCandidate[]> _candidatesGroupedByContainingSystemTypes;

        private Dictionary<TypeDeclarationSyntax, QueryCandidate[]> CandidatesGroupedByContainingSystemTypes
        {
            get
            {
                if (_candidatesGroupedByContainingSystemTypes == null)
                {
                    _candidatesGroupedByContainingSystemTypes =
                        _queryCandidates
                            .GroupBy(c => c.ContainingTypeNode)
                            .ToDictionary(group => group.Key, group => group.ToArray());
                }
                return _candidatesGroupedByContainingSystemTypes;
            }
        }

        public IEnumerable<(SyntaxNode SyntaxNode, TypeDeclarationSyntax SystemType)> Candidates
            => _queryCandidates.Select(candidate => (SyntaxNode: candidate.FullInvocationChainSyntaxNode, ContainingType: candidate.ContainingTypeNode));

        public bool RequiresReferenceToBurst { get; private set; }

        public void OnReceiveSyntaxNode(SyntaxNode node)
        {
            if (node is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                switch (invocationExpressionSyntax.Expression)
                {
                    case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
                    {
                        switch (memberAccessExpressionSyntax.Name)
                        {
                            case GenericNameSyntax { Identifier: { ValueText: "Query" } } genericNameSyntax:
                            {
                                var candidate = QueryCandidate.From(invocationExpressionSyntax, genericNameSyntax.TypeArgumentList.Arguments);
                                _queryCandidates.Add(candidate);
                                break;
                            }
                        }
                        break;
                    }
                    case GenericNameSyntax { Identifier: { ValueText: "Query" } } genericNameSyntax:
                    {
                        var candidate = QueryCandidate.From(invocationExpressionSyntax, genericNameSyntax.TypeArgumentList.Arguments);
                        _queryCandidates.Add(candidate);
                        break;
                    }
                }
            }
        }

        public bool RegisterChangesInSystem(SystemDescription systemDescription)
        {
            var idiomaticCSharpForEachDescriptions = new List<IdiomaticCSharpForEachDescription>();
            foreach (var queryCandidate in CandidatesGroupedByContainingSystemTypes[systemDescription.SystemTypeSyntax])
            {
                var description = new IdiomaticCSharpForEachDescription(systemDescription, queryCandidate, idiomaticCSharpForEachDescriptions.Count);

                description.Success &=
                    VerifyQueryTypeCorrectness(description.SystemDescription, description.Location, description.AllQueryTypes, invokedMethodName: "WithAll");
                description.Success &=
                    VerifyQueryTypeCorrectness(description.SystemDescription, description.Location, description.NoneQueryTypes, invokedMethodName: "WithNone");
                description.Success &=
                    VerifyQueryTypeCorrectness(description.SystemDescription, description.Location, description.AnyQueryTypes, invokedMethodName: "WithAny");

                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.AbsentQueryTypes, description.DisabledQueryTypes, "WithAbsent", "WithDisabled");
                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.AbsentQueryTypes, description.AllQueryTypes, "WithAbsent", "WithAll");
                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.AbsentQueryTypes, description.AnyQueryTypes, "WithAbsent", "WithAny");
                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.NoneQueryTypes, description.AllQueryTypes, "WithNone", "WithAll");
                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.NoneQueryTypes, description.AnyQueryTypes, "WithNone", "WithAny");
                description.Success &=
                    VerifyNoMutuallyExclusiveQueries(description.SystemDescription, description.Location, description.AnyQueryTypes, description.AllQueryTypes, "WithAny", "WithAll");

                if (!description.Success) // if !description.Success, TypeSymbolsForEntityQueryCreation might not be right, thus causing an exception
                    continue;

                description.Success &= VerifyNoMutuallyExclusiveQueries(
                    description.SystemDescription,
                    description.Location,
                    description.AbsentQueryTypes,
                    description.IterableNonEnableableTypes,
                    "WithAbsent",
                    "Main query type in SystemAPI.Query",
                    compareTypeSymbolsOnly: true);

                description.Success &= VerifyNoMutuallyExclusiveQueries(
                    description.SystemDescription,
                    description.Location,
                    description.DisabledQueryTypes,
                    description.IterableNonEnableableTypes,
                    "WithDisabled",
                    "Main query type in SystemAPI.Query",
                    compareTypeSymbolsOnly: true);

                description.Success &= VerifyNoMutuallyExclusiveQueries(
                    description.SystemDescription,
                    description.Location,
                    description.NoneQueryTypes,
                    description.IterableNonEnableableTypes,
                    "WithNone",
                    "Main query type in SystemAPI.Query",
                    compareTypeSymbolsOnly: true);

                description.Success &= VerifyNoMutuallyExclusiveQueries(
                    description.SystemDescription,
                    description.Location,
                    description.AnyQueryTypes,
                    description.IterableNonEnableableTypes,
                    "WithAny",
                    "Main query type in SystemAPI.Query",
                    compareTypeSymbolsOnly: true);

                if (description.Success)
                {
                    if (description.IsBurstEnabled)
                        RequiresReferenceToBurst = true;

                    idiomaticCSharpForEachDescriptions.Add(description);
                }
            }
            foreach (var description in idiomaticCSharpForEachDescriptions)
            {
                if (description.IfeType.IsGenerated)
                {
                    systemDescription.NewMiscellaneousMembers.Add(description.IfeType.Value.StructDeclarationSyntax);

                    description.ContainerOrAspectTypeHandleFieldName =
                        systemDescription.HandlesDescription.GetOrCreateSourceGeneratedTypeHandleField(description.IfeType.Value.FullyQualifiedTypeName);
                }
                else
                {
                    // We do not generate container types when querying a single Aspect without `.WithEntityAccess()`
                    description.ContainerOrAspectTypeHandleFieldName =
                        systemDescription.HandlesDescription.GetOrCreateTypeHandleField(description.AllIterableQueryDatas.Single().TypeSymbol, isReadOnly: false);

                    if (description.RequiresAspectLookupField)
                    {
                        description.AspectLookupTypeHandleFieldName = systemDescription.HandlesDescription.GetOrCreateAspectLookup(description.AllIterableQueryDatas.Single().TypeSymbol, isReadOnly: false);
                    }
                }
                description.SourceGeneratedEntityQueryFieldName =
                    systemDescription.HandlesDescription.GetOrCreateQueryField(
                        new SingleArchetypeQueryFieldDescription(
                            new Archetype(
                                description.GetDistinctAllQueryTypes(),
                                description.AnyQueryTypes,
                                description.NoneQueryTypes,
                                description.DisabledQueryTypes,
                                description.AbsentQueryTypes,
                                description.GetEntityQueryOptionsArgument()),
                            description.ChangeFilterQueryTypes));
            }

            systemDescription.Rewriters.Add(new SystemAPIQueryRewriter(idiomaticCSharpForEachDescriptions));
            return true;
        }
    }
}
