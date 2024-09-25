using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.SystemAPI.QueryBuilder
{
    public class SystemAPIQueryBuilderRewriter : SystemRewriter
    {
        private readonly IReadOnlyCollection<SystemAPIQueryBuilderDescription> _systemApiQueryBuilderDescriptions;
        private readonly Dictionary<SyntaxNode,SystemAPIQueryBuilderDescription> _queryBuilderInvocationNodeToDescription;
        private bool _replacedNode;

        public override IEnumerable<SyntaxNode> NodesToTrack =>
            _systemApiQueryBuilderDescriptions.Select(d => d.NodeToReplace);

        public SystemAPIQueryBuilderRewriter(IReadOnlyCollection<SystemAPIQueryBuilderDescription> systemApiQueryBuilderDescriptions)
        {
            _systemApiQueryBuilderDescriptions = systemApiQueryBuilderDescriptions;
            _queryBuilderInvocationNodeToDescription = new Dictionary<SyntaxNode, SystemAPIQueryBuilderDescription>();
        }


        // By the time this method is invoked, the system has already been rewritten at least once.
        // In other words, the `systemRootNode` argument passed to this method is the root node of the REWRITTEN system --
        // i.e., a copy of the original system with changes applied.
        public override SyntaxNode VisitTrackedSystem(SyntaxNode systemRootNode, string originalFilePath)
        {
            m_OriginalFilePath = originalFilePath;

            foreach (var description in _systemApiQueryBuilderDescriptions)
            {
                var rewrittenQueryInvocationNode = systemRootNode.GetCurrentNodes(description.NodeToReplace).FirstOrDefault() ?? description.NodeToReplace;
                _queryBuilderInvocationNodeToDescription.Add(rewrittenQueryInvocationNode, description);
            }
            return Visit(systemRootNode);
        }

        public override SyntaxNode Visit(SyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
                return null;

            var replacedNodeAndChildren = base.Visit(syntaxNode);

            // If the current node is a node we want to replace
            if (_queryBuilderInvocationNodeToDescription.TryGetValue(syntaxNode, out var description))
            {
                // Replace the current node
                replacedNodeAndChildren = description.GetReplacementNode();
                _replacedNode = true;
            }

            // If we have performed any replacements, we need to update the `RewrittenMemberHashCodeToSyntaxNode` dictionary accordingly
            else if (replacedNodeAndChildren is MemberDeclarationSyntax memberDeclarationSyntax && _replacedNode)
            {
                RecordChangedMember(memberDeclarationSyntax);
                _replacedNode = false;
            }

            return replacedNodeAndChildren;
        }
    }
}
