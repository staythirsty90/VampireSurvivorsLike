using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI.Query
{
    public partial class IdiomaticCSharpForEachModule
    {
        class SystemAPIQueryRewriter : SystemRewriter
        {
            struct RewrittenSystemAPIQueryInvocation
            {
                public SyntaxNode ReplacementNode;
                public IdiomaticCSharpForEachDescription Description;
                public int OriginalForEachStatementLineNumber;
            }

            readonly Dictionary<SyntaxNode, RewrittenSystemAPIQueryInvocation> _rewrittenNodes;
            readonly Dictionary<IdiomaticCSharpForEachDescription, (SyntaxNode Original, SyntaxNode Replacement)[]> _descriptionsToReplacementLookups;
            readonly IReadOnlyCollection<IdiomaticCSharpForEachDescription> _descriptions;
            readonly Dictionary<SyntaxNode, (HashSet<string> statementSnippets, int originalLineNumber)> _targetAndStatementsToInsertBefore;
            readonly Dictionary<SyntaxNode, HashSet<string>> _targetAndStatementsToInsertAfter;
            bool _replacedNode;

            public override IEnumerable<SyntaxNode> NodesToTrack =>
                _descriptionsToReplacementLookups.SelectMany(kvp => kvp.Value).Select(kvp => kvp.Original);

            public SystemAPIQueryRewriter(IReadOnlyCollection<IdiomaticCSharpForEachDescription> descriptionsInSameSystemType)
            {
                _descriptions = descriptionsInSameSystemType;
                _targetAndStatementsToInsertBefore = new Dictionary<SyntaxNode, (HashSet<string>, int)>();
                _targetAndStatementsToInsertAfter = new Dictionary<SyntaxNode, HashSet<string>>();
                _rewrittenNodes = new Dictionary<SyntaxNode, RewrittenSystemAPIQueryInvocation>();
                _descriptionsToReplacementLookups =
                    descriptionsInSameSystemType.ToDictionary(d => d, d => d.GetOriginalNodesToReplacementNodes().ToArray());
            }

            // By the time this method is invoked, the system has already been rewritten at least once.
            // In other words, the `systemRootNode` argument passed to this method is the root node of the REWRITTEN system --
            // i.e., a copy of the original system with changes applied.
            public override SyntaxNode VisitTrackedSystem(SyntaxNode systemRootNode, string originalFilePath)
            {
                m_OriginalFilePath = originalFilePath;

                foreach (var description in _descriptions)
                {
                    var replacements = _descriptionsToReplacementLookups[description];

                    var originalNodes = replacements.Select(kvp => kvp.Original).ToArray();
                    var replacementNodes = replacements.Select(kvp => kvp.Replacement).ToArray();

                    var nodesRewrittenDuringTracking = (systemRootNode.GetCurrentNodes(originalNodes) ?? originalNodes).ToArray();

                    for (int i = 0; i < nodesRewrittenDuringTracking.Length; i++)
                    {
                        _rewrittenNodes.Add(
                            nodesRewrittenDuringTracking[i],
                            new RewrittenSystemAPIQueryInvocation
                            {
                                Description = description,
                                OriginalForEachStatementLineNumber = originalNodes[i].GetLineNumber(),
                                ReplacementNode = replacementNodes[i]
                            });
                    }
                }
                return Visit(systemRootNode);
            }

            public override SyntaxNode Visit(SyntaxNode syntaxNode)
            {
                if (syntaxNode == null)
                    return null;

                var replacedNodeAndChildren = base.Visit(syntaxNode);

                // If the current node is a node we want to replace -- e.g. `SystemAPI.Query<MyComponent>()`
                if (_rewrittenNodes.TryGetValue(syntaxNode, out var rewrittenData))
                {
                    // Replace the current node
                    replacedNodeAndChildren = rewrittenData.ReplacementNode;
                    _replacedNode = true;

                    var containingForEachStatement = syntaxNode.Ancestors().OfType<CommonForEachStatementSyntax>().First();

                    if (!_targetAndStatementsToInsertBefore.ContainsKey(containingForEachStatement))
                        _targetAndStatementsToInsertBefore[containingForEachStatement] = (new HashSet<string>(), rewrittenData.OriginalForEachStatementLineNumber);

                    foreach (var newStatement in rewrittenData.Description.GetStatementsToInsertBeforeForEachIteration())
                        _targetAndStatementsToInsertBefore[containingForEachStatement].statementSnippets.Add(newStatement);

                    var statementsToInsertAfterForEachIteration = rewrittenData.Description.GetStatementsToInsertAfterForEachIteration().ToArray();
                    if (statementsToInsertAfterForEachIteration.Length > 0)
                    {
                        if (!_targetAndStatementsToInsertAfter.ContainsKey(containingForEachStatement))
                            _targetAndStatementsToInsertAfter[containingForEachStatement.Statement] = new HashSet<string>();

                        foreach (var newStatement in statementsToInsertAfterForEachIteration)
                            _targetAndStatementsToInsertAfter[containingForEachStatement.Statement].Add(newStatement);
                    }
                }

                // If we want to insert additional statements
                if (replacedNodeAndChildren is StatementSyntax statementSyntax)
                {
                    if (_targetAndStatementsToInsertBefore.TryGetValue(syntaxNode, out (HashSet<string> statementSnippets, int originalLineNumber) statementsToInsertBefore))
                    {
                        var statements =
                            new List<StatementSyntax>(
                                statementsToInsertBefore.statementSnippets.Select(s =>
                                    (StatementSyntax)SyntaxFactory.ParseStatement(s).WithHiddenLineTrivia()))
                            {
                                (StatementSyntax)statementSyntax.WithHiddenLineTrivia()
                            };
                        statements[0] = statements[0].WithLineTrivia(m_OriginalFilePath, statementsToInsertBefore.originalLineNumber) as StatementSyntax;

                        replacedNodeAndChildren =
                            SyntaxFactory.Block(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                                new SyntaxList<StatementSyntax>(statements),
                                SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));
                    }

                    else if (_targetAndStatementsToInsertAfter.TryGetValue(syntaxNode, out HashSet<string> statementsToInsertAfter))
                    {
                        var statements = new List<StatementSyntax> { statementSyntax.WithoutTrivia() };
                        statements.AddRange(statementsToInsertAfter.Select(s => (StatementSyntax)SyntaxFactory.ParseStatement(s).WithHiddenLineTrivia()));

                        replacedNodeAndChildren =
                            SyntaxFactory.Block(
                                new SyntaxList<AttributeListSyntax>(),
                                SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                                new SyntaxList<StatementSyntax>(statements),
                                SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));
                    }
                }

                // If we have performed any replacements, we need to update the `RewrittenMemberHashCodeToSyntaxNode` dictionary accordingly
                if (replacedNodeAndChildren is MemberDeclarationSyntax memberDeclarationSyntax && _replacedNode)
                {
                    RecordChangedMember(memberDeclarationSyntax);
                    _replacedNode = false;
                }
                return replacedNodeAndChildren;
            }
        }
    }
}
