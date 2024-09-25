using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI.QueryBuilder
{
    public class SystemAPIQueryBuilderModule : ISystemModule
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
            => _queryCandidates.Select(candidate => (SyntaxNode: candidate.BuildNode, ContainingType: candidate.ContainingTypeNode));

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
                            case IdentifierNameSyntax { Identifier: { ValueText: "QueryBuilder" } } identifierNameSyntax:
                            {
                                var (success, result) = QueryCandidate.TryCreateFrom(invocationExpressionSyntax);
                                if (success)
                                    _queryCandidates.Add(result);
                                break;
                            }
                        }
                        break;
                    }
                    case IdentifierNameSyntax { Identifier: { ValueText: "QueryBuilder" } } identifierNameSyntax:
                    {
                        var (success, result) = QueryCandidate.TryCreateFrom(invocationExpressionSyntax);
                        if (success)
                            _queryCandidates.Add(result);
                        break;
                    }
                }
            }
        }

        readonly string[] _groupNames = { "WithAll", "WithAny", "WithNone", "WithDisabled", "WithAbsent" };

        public bool RegisterChangesInSystem(SystemDescription systemDescription)
        {
            var systemApiQueryBuilderDescriptions = new List<SystemAPIQueryBuilderDescription>();
            foreach (var queryCandidate in CandidatesGroupedByContainingSystemTypes[systemDescription.SystemTypeSyntax])
            {
                var description = new SystemAPIQueryBuilderDescription(systemDescription, queryCandidate);

                foreach (var archetypeInfo in description.Archetypes.Zip(description.QueryFinalizingLocations, (a, l) => (Archetype: a, Location: l)))
                {
                    // must match order of groupNames above
                    var groupLists = new []{
                        archetypeInfo.Archetype.All,
                        archetypeInfo.Archetype.Any,
                        archetypeInfo.Archetype.None,
                        archetypeInfo.Archetype.Disabled,
                        archetypeInfo.Archetype.Absent,
                    };

                    for (int i = 0; i < _groupNames.Length; ++i)
                    {
                        description.Success &=
                            QueryVerification.VerifyQueryTypeCorrectness(
                                description.SystemDescription,
                                archetypeInfo.Location,
                                groupLists[i],
                                invokedMethodName: _groupNames[i]);
                        for (int j = i + 1; j < _groupNames.Length; ++j)
                        {
                            description.Success &=
                                QueryVerification.VerifyNoMutuallyExclusiveQueries(
                                    description.SystemDescription,
                                    archetypeInfo.Location,
                                    groupLists[i],
                                    groupLists[j],
                                    _groupNames[i],
                                    _groupNames[j]);
                        }
                    }
                }

                if (!description.Success)
                    continue;

                if (description.IsBurstEnabled)
                    RequiresReferenceToBurst = true;

                systemApiQueryBuilderDescriptions.Add(description);
            }
            foreach (var description in systemApiQueryBuilderDescriptions)
            {
                description.GeneratedEntityQueryFieldName
                    = systemDescription.HandlesDescription.GetOrCreateQueryField(new MultipleArchetypeQueryFieldDescription(description.Archetypes.ToArray(), description.GetQueryBuilderBodyBeforeBuild()));
            }

            systemDescription.Rewriters.Add(new SystemAPIQueryBuilderRewriter(systemApiQueryBuilderDescriptions));
            return true;
        }
    }
}
