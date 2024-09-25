using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Unity.Entities.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EntitiesCodeFixProvider)), Shared]
    public class EntitiesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            EntitiesDiagnostics.ID_EA0001,
            EntitiesDiagnostics.ID_EA0007,
            EntitiesDiagnostics.ID_EA0008,
            EntitiesDiagnostics.ID_EA0009,
            EntitiesDiagnostics.ID_EA0010);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root?.FindNode(diagnostic.Location.SourceSpan);
                if (node is LocalDeclarationStatementSyntax {Declaration:{} variableDeclaration} && diagnostic.Id == EntitiesDiagnostics.ID_EA0001) {
                        context.RegisterCodeFix(
                            CodeAction.Create(title: "Use non-readonly reference",
                                createChangedDocument: c => MakeNonReadonlyReference(context.Document, variableDeclaration, c),
                                equivalenceKey: "NonReadonlyReference"),
                            diagnostic);
                }

                if (node is ParameterSyntax parameterSyntax && diagnostic.Id == EntitiesDiagnostics.ID_EA0009) {
                        context.RegisterCodeFix(
                            CodeAction.Create(title: "Use non-readonly reference",
                                createChangedDocument: c => MakeNonReadonlyReference(context.Document, parameterSyntax, c),
                                equivalenceKey: "NonReadonlyReferenceParameter"),
                            diagnostic);
                }

                if (node is TypeDeclarationSyntax typeDeclarationSyntax) {
                        if (diagnostic.Id == EntitiesDiagnostics.ID_EA0007 || diagnostic.Id == EntitiesDiagnostics.ID_EA0008)
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(title: "Add partial keyword",
                                    createChangedDocument: c => AddPartial(context.Document, typeDeclarationSyntax, c),
                                    equivalenceKey: "AddPartial"),
                                diagnostic);
                        }
                        else if (diagnostic.Id == EntitiesDiagnostics.ID_EA0010)
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(title: "Add BurstCompile attribute",
                                    createChangedDocument: c => AddBurstCompilerAttribute(context.Document, typeDeclarationSyntax, c),
                                    equivalenceKey: "AddBurstCompilerAttribute"),
                                diagnostic);
                        }
            }
        }
        }

        static async Task<Document> AddPartial(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
        {
            var partialModifier = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
            var modifiedSyntax = typeDeclarationSyntax.WithoutLeadingTrivia().AddModifiers(partialModifier).WithTriviaFrom(typeDeclarationSyntax);
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(oldRoot != null, nameof(oldRoot) + " != null");
            var newRoot = oldRoot.ReplaceNode(typeDeclarationSyntax, modifiedSyntax);
            return document.WithSyntaxRoot(newRoot);
        }

        static async Task<Document> MakeNonReadonlyReference(Document document, ParameterSyntax parameterSyntax, CancellationToken cancellationToken)
        {
            var modifiedParam = parameterSyntax;
            modifiedParam = modifiedParam.WithoutLeadingTrivia();
            modifiedParam = modifiedParam.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
            modifiedParam = modifiedParam.WithTriviaFrom(parameterSyntax);
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(parameterSyntax, modifiedParam);
            return document.WithSyntaxRoot(newRoot);
        }

        static async Task<Document> MakeNonReadonlyReference(Document document, VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            var modifiedVarDecl = variableDeclaration;
            if (modifiedVarDecl.Type is RefTypeSyntax overallRefType)
            {
                // fixes snippets with readonly keyword e.g. `ref readonly MyBlob readonlyBlob = ref _blobAssetReference.Value`
                modifiedVarDecl = modifiedVarDecl.WithType(overallRefType.WithReadOnlyKeyword(default).WithTriviaFrom(overallRefType));
            }
            else
            {
                // fixes snippets missing ref keywords e.g. `MyBlob readonlyBlob = _blobAssetReference.Value`
                var originalTypeWithSpace = modifiedVarDecl.Type.WithoutTrivia();
                var type = SyntaxFactory.RefType(originalTypeWithSpace).WithTriviaFrom(modifiedVarDecl.Type);
                type = type.WithRefKeyword(type.RefKeyword.WithTrailingTrivia(SyntaxFactory.Space));
                modifiedVarDecl = modifiedVarDecl.WithType(type);

                var modifiedVariables = modifiedVarDecl.Variables.ToArray();
                for (var index = 0; index < modifiedVariables.Length; index++)
                {
                    var modifiedValue = SyntaxFactory.RefExpression(modifiedVariables[index].Initializer.Value.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space)).WithTriviaFrom(modifiedVariables[index].Initializer.Value);
                    modifiedVariables[index] = modifiedVariables[index].WithInitializer(modifiedVariables[index].Initializer.WithValue(modifiedValue));
                }
                modifiedVarDecl = modifiedVarDecl.WithVariables(SyntaxFactory.SeparatedList(modifiedVariables));
            }
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(variableDeclaration, modifiedVarDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        static async Task<Document> AddBurstCompilerAttribute(Document document,
            TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
        {
            var burstCompilerAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("BurstCompile"));
            var burstCompilerAttributeList =
                SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {burstCompilerAttribute}))
                    .NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.LineFeed)
                    .WithLeadingTrivia(typeDeclarationSyntax.GetLeadingTrivia());

            var modifiedSyntax = typeDeclarationSyntax.AddAttributeLists(burstCompilerAttributeList);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(oldRoot != null, nameof(oldRoot) + " != null");

            var newRoot = oldRoot.ReplaceNode(typeDeclarationSyntax, modifiedSyntax);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
