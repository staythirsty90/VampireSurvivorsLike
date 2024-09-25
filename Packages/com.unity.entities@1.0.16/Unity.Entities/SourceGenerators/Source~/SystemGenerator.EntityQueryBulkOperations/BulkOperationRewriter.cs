using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.SystemGenerator.Common;

namespace Unity.Entities.SourceGen
{
    public class BulkOperationRewriter : SystemRewriter
    {
        private readonly Dictionary<SyntaxNode, SyntaxNode> _rewrittenNodeToReplacementNode;
        private readonly IDictionary<InvocationExpressionSyntax, SyntaxNode> _originalNodeToReplacementNode;
        private bool _replacedNode;

        public override IEnumerable<SyntaxNode> NodesToTrack => _originalNodeToReplacementNode.Keys;

        public BulkOperationRewriter(IDictionary<InvocationExpressionSyntax, SyntaxNode> originalNodeToReplacementNode)
        {
            _rewrittenNodeToReplacementNode = new Dictionary<SyntaxNode, SyntaxNode>();
            _originalNodeToReplacementNode = originalNodeToReplacementNode;
        }

        public override SyntaxNode VisitTrackedSystem(SyntaxNode systemRootNode, string originalFilePath)
        {
            var originalNodes = _originalNodeToReplacementNode.Select(kvp => kvp.Key).ToArray();
            var replacementNodes = _originalNodeToReplacementNode.Select(kvp => kvp.Value).ToArray();
            var nodesRewrittenDuringTracking = (systemRootNode.GetCurrentNodes(originalNodes) ?? originalNodes).ToArray();

            for (int i = 0; i < nodesRewrittenDuringTracking.Length; i++)
                _rewrittenNodeToReplacementNode.Add(nodesRewrittenDuringTracking[i], replacementNodes[i]);

            return Visit(systemRootNode);
        }

        public override SyntaxNode Visit(SyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
                return null;

            var replacedNodeAndChildren = base.Visit(syntaxNode);

            if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax
                && _rewrittenNodeToReplacementNode.TryGetValue(invocationExpressionSyntax, out var replacementNode))
            {
                replacedNodeAndChildren = replacementNode;
                _replacedNode = true;
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
