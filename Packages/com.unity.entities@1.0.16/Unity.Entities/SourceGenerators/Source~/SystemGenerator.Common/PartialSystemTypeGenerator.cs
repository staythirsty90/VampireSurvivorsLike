using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Entities.SourceGen.Common;
using static Unity.Entities.SourceGen.Common.SourceGenHelpers;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public static class PartialSystemTypeGenerator
    {
        public static (SyntaxTreeInfo SyntaxTreeInfo, TypeDeclarationSyntax OriginalSystem, TypeDeclarationSyntax GeneratedPartialSystem)
            Generate(SystemDescription[] allDescriptionsForTheSameSystem)
        {
            var description = allDescriptionsForTheSameSystem.First();

            var generatedPartialType = CreatePartialSystemType(allDescriptionsForTheSameSystem, description.SystemTypeSyntax);
            generatedPartialType = ReplaceNodesInSystem(generatedPartialType, allDescriptionsForTheSameSystem);
            generatedPartialType = AddMiscellaneousMembers(generatedPartialType, allDescriptionsForTheSameSystem);
            generatedPartialType = AddEntityCommandBufferSystemFields(generatedPartialType, allDescriptionsForTheSameSystem);
            generatedPartialType = AddOnCreateForCompilerWithFields(generatedPartialType, allDescriptionsForTheSameSystem, description.SystemType == SystemType.ISystem);

            return (description.SyntaxTreeInfo, description.SystemTypeSyntax, generatedPartialType);
        }

        private static TypeDeclarationSyntax AddMiscellaneousMembers(TypeDeclarationSyntax generatedPartialType, SystemDescription[] descriptions)
            => generatedPartialType.AddMembers(descriptions.SelectMany(d=>d.NewMiscellaneousMembers).ToArray());

        private static TypeDeclarationSyntax AddEntityCommandBufferSystemFields(TypeDeclarationSyntax generatedPartialType, SystemDescription[] descriptions)
        {
            foreach (var kvp in descriptions.SelectMany(d => d.FullEcbSystemTypeNamesToGeneratedFieldNames).DistinctBy(kvp => kvp.Key))
            {
                var field = SyntaxFactory.ParseMemberDeclaration($"{kvp.Key} {kvp.Value};");
                if (field != null)
                    generatedPartialType = generatedPartialType.AddMembers(field);
            }

            return generatedPartialType;
        }

        private static TypeDeclarationSyntax AddOnCreateForCompilerWithFields(TypeDeclarationSyntax generatedPartialType, SystemDescription[] descriptions, bool isISystem)
        {
            // Only needs to create OnCreateForCompiler in SystemBase if members are present
            if (!descriptions.Any(d => d.HandlesDescription.NonQueryFields.Count > 0 || d.HandlesDescription.QueryFieldsToFieldNames.Count > 0 || d.AdditionalStatementsInOnCreateForCompilerMethod.Count > 0))
                return isISystem ? generatedPartialType.AddMembers(EntitiesSourceFactory.OnCreateForCompilerStub()) : generatedPartialType;

            var typeHandle = HandlesDescription.GetTypeHandleForInitialPartial(false, false, "", descriptions.Select(d=>d.HandlesDescription).ToArray());
            var typeHandleSyntaxMember = SyntaxFactory.ParseMemberDeclaration(typeHandle);
            if (typeHandleSyntaxMember is TypeDeclarationSyntax typeHandleSyntax)
                generatedPartialType = generatedPartialType.AddMembers(typeHandleSyntax.Members.ToArray());

            var additionalSyntax = descriptions.SelectMany(d => d.AdditionalStatementsInOnCreateForCompilerMethod).Distinct().ToArray();
            generatedPartialType = generatedPartialType.AddMembers(EntitiesSourceFactory.OnCreateForCompilerMethod(additionalSyntax, isISystem));

            return generatedPartialType;
        }

        private static TypeDeclarationSyntax CreatePartialSystemType(SystemDescription[] descriptions, TypeDeclarationSyntax originalSyntax)
        {
            var allFullyQualifiedBaseTypeNames = descriptions.SelectMany(desc => desc.FullyQualifiedBaseTypeNames).Distinct();

            TypeDeclarationSyntax generatedPartialType;

            switch (originalSyntax)
            {
                case StructDeclarationSyntax _:
                    generatedPartialType = SyntaxFactory.StructDeclaration(originalSyntax.Identifier);
                    allFullyQualifiedBaseTypeNames = allFullyQualifiedBaseTypeNames.Append("global::Unity.Entities.ISystemCompilerGenerated");
                    break;
                default:
                    generatedPartialType = SyntaxFactory.ClassDeclaration(originalSyntax.Identifier);
                    break;
            }

            var baseTypeSyntaxNodes = allFullyQualifiedBaseTypeNames.Select(baseTypeName => SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseTypeName)));
            var baseListSyntax = SyntaxFactory.BaseList().WithTypes(new SeparatedSyntaxList<BaseTypeSyntax>().AddRange(baseTypeSyntaxNodes));

            generatedPartialType =
                generatedPartialType
                    .WithBaseList(baseListSyntax)
                    .WithModifiers(originalSyntax.Modifiers)
                    .WithAttributeLists(GetCompilerGeneratedAttribute());

            if (originalSyntax.TypeParameterList != null)
                generatedPartialType = generatedPartialType.WithTypeParameterList(originalSyntax.TypeParameterList);

            return generatedPartialType;
        }

        public static SyntaxTokenList RemovePartialModifier(SyntaxTokenList tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsKind(SyntaxKind.PartialKeyword))
                    return tokens.RemoveAt(i);
            }

            return tokens;
        }

        private static TypeDeclarationSyntax ReplaceNodesInSystem(TypeDeclarationSyntax generatedPartialType, IReadOnlyList<SystemDescription> systemDescriptions)
        {
            foreach (var description in systemDescriptions)
                if (description.Rewriters.Any() || description.NonNestedReplacementsInMethods.Count != 0)
                    description.Rewriters.Insert(0,new CorrectionSystemReplacer(description.NonNestedReplacementsInMethods));

            /*
             Note: `GetTrackedSystems(systemDescriptions)` returns COPIES of the `TypeDeclarationSyntax` nodes we are tracking.
             In fact, every single time we modify a node, we get back A COPY of the original node, rather than the original node itself.
             This is because syntax nodes are immutable by nature. Whenever we track a node, it gets annotated with the string literal "Id" --
             i.e., we are in fact modifying the node. Thus, every time we track a node, we get back a copy of the original node with the new annotation applied.
            */
            var trackedSystems = GetTrackedSystems(systemDescriptions).ToArray();
            var rewrittenMemberToSyntaxNode = new Dictionary<SyntaxAnnotation, MemberDeclarationSyntax>();
            for (var i = 0; i < systemDescriptions.Count; i++)
            {
                var description = systemDescriptions[i];
                // Case hit when no candidates are found in all modules of the given system description.
                if (!description.Rewriters.Any())
                    continue;
                var originalFilePath = description.SyntaxTreeInfo.Tree.FilePath.Replace('\\', '/');

                SyntaxNode rewrittenSystem = trackedSystems[i];
                // Iterate through all rewriters. All rewriters will update their own `RewrittenMemberHashCodeToSyntaxNode` dictionaries during the rewrite process.
                foreach (var rewriter in description.Rewriters)
                {
                    if (rewriter is CorrectionSystemReplacer correctionSystemReplacer)
                        correctionSystemReplacer.SetOffsetLineNumber(description.SystemTypeSyntax.GetLineNumber()+1);
                    rewrittenSystem = rewriter.VisitTrackedSystem(rewrittenSystem, originalFilePath);
                }
            }

            // All rewriters have finished running
            for (var index = 0; index < systemDescriptions.Count; index++)
            {
                var systemDescription = systemDescriptions[index];
                var annotationsToOriginalSyntaxNode = systemDescription.GetAnnotationsToOriginalSyntaxNodes(trackedSystems[index]);

                // As mentioned previously, all rewriters update their own `RewrittenMemberHashCodeToSyntaxNode` dictionaries during the rewrite process.
                // By the time the next line is executed, all `RewrittenMemberHashCodeToSyntaxNode` dictionaries should contain all information about all rewritten nodes.
                var systemRewriterRewrittenMemberAnnotationToSyntaxNode = new Dictionary<SyntaxAnnotation, MemberDeclarationSyntax>();
                foreach (var rewriter in systemDescription.Rewriters)
                foreach (var kvp in rewriter.RewrittenMemberAnnotationToSyntaxNode)
                    systemRewriterRewrittenMemberAnnotationToSyntaxNode[kvp.Key] = kvp.Value;

                foreach (var kvp in systemRewriterRewrittenMemberAnnotationToSyntaxNode)
                {
                    var rewrittenMemberAnnotation = kvp.Key;
                    var rewrittenMemberSyntaxNode = kvp.Value;

                    // If our rewriters have modified a new method or property, we need to ensure that the rewritten method/property has the right identifier name, attribute and modifiers.
                    switch (rewrittenMemberSyntaxNode)
                    {
                        case MethodDeclarationSyntax rewrittenMethod:
                        {
                            var originalMethodDeclarationSyntax = (MethodDeclarationSyntax)annotationsToOriginalSyntaxNode[rewrittenMethod.GetAnnotations(TrackedNodeAnnotationUsedByRoslyn).First()];
                            var originalMethodSymbol = systemDescription.SemanticModel.GetDeclaredSymbol(originalMethodDeclarationSyntax);

                            var targetMethodNameAndSignature = originalMethodSymbol.GetMethodAndParamsAsString();
                            var stableHashCode = GetStableHashCode($"_{targetMethodNameAndSignature}") & 0x7fffffff;
                            var (modifiers, attributeList) = GetModifiersAndAttributes(targetMethodNameAndSignature, rewrittenMemberSyntaxNode, isMethod: true);
                            modifiers = RemovePartialModifier(modifiers);

                            rewrittenMemberSyntaxNode =
                                rewrittenMethod
                                    .WithExplicitInterfaceSpecifier(null)
                                    .WithoutPreprocessorTrivia()
                                    .WithIdentifier(SyntaxFactory.Identifier($"__{rewrittenMethod.Identifier.ValueText}_{stableHashCode:X}"))
                                    .WithModifiers(modifiers)
                                    .WithAttributeLists(attributeList);
                            break;
                        }
                        case PropertyDeclarationSyntax rewrittenProperty:
                        {
                            var originalPropertyDeclarationSyntaxNode = (PropertyDeclarationSyntax)annotationsToOriginalSyntaxNode[rewrittenProperty.GetAnnotations(TrackedNodeAnnotationUsedByRoslyn).First()];
                            var originalPropertySymbol = systemDescription.SemanticModel.GetDeclaredSymbol(originalPropertyDeclarationSyntaxNode);

                            var targetPropertyAndSignature = originalPropertySymbol.OriginalDefinition.ToString();
                            var stableHashCode = GetStableHashCode($"_{targetPropertyAndSignature}") & 0x7fffffff;
                            var (modifiers, attributeList) = GetModifiersAndAttributes(targetPropertyAndSignature, rewrittenMemberSyntaxNode, isMethod: false);
                            modifiers = RemovePartialModifier(modifiers);

                            rewrittenMemberSyntaxNode =
                                rewrittenProperty
                                    .WithoutPreprocessorTrivia()
                                    .WithIdentifier(SyntaxFactory.Identifier($"__{rewrittenProperty.Identifier.ValueText}_{stableHashCode:X}"))
                                    .WithModifiers(modifiers)
                                    .WithAttributeLists(attributeList);
                            break;
                        }
                    }

                    rewrittenMemberToSyntaxNode[rewrittenMemberAnnotation] = rewrittenMemberSyntaxNode;
                }
            }

            return generatedPartialType.WithMembers(new SyntaxList<MemberDeclarationSyntax>(rewrittenMemberToSyntaxNode.Values));
        }

        static IEnumerable<TypeDeclarationSyntax> GetTrackedSystems(IEnumerable<SystemDescription> descriptions)
        {
            foreach (var description in descriptions)
            {
                // `TrackNodes()` returns a new copy of the system, in which tracked nodes are annotated with the string literal "Id".
                var trackedSystem =
                    description.SystemTypeSyntax.TrackNodes(
                        description.Rewriters
                            .SelectMany(rewriter => rewriter.NodesToTrack)
                            .Concat(description.SystemTypeSyntax.Members));

                yield return trackedSystem;
            }
        }

        static (SyntaxTokenList, SyntaxList<AttributeListSyntax>) GetModifiersAndAttributes(string targetMethodNameAndSignature, MemberDeclarationSyntax rewritten, bool isMethod)
        {
            // Make sure emit new method as private so that containing type can be sealed
            var modifiers = new SyntaxTokenList(rewritten.Modifiers.Where(
                m => !(m.IsKind(SyntaxKind.OverrideKeyword) || m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) ||
                       m.IsKind(SyntaxKind.VirtualKeyword) || m.IsKind(SyntaxKind.AbstractKeyword))));

            var dotsCompilerPatchedArguments = SyntaxFactory.ParseAttributeArgumentList($"(\"{targetMethodNameAndSignature}\")");
            var attributeName = isMethod ? "global::Unity.Entities.DOTSCompilerPatchedMethod" : "global::Unity.Entities.DOTSCompilerPatchedProperty";

            var dotsCompilerPatchedMethodAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName), dotsCompilerPatchedArguments);
            return (modifiers, new SyntaxList<AttributeListSyntax>(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {dotsCompilerPatchedMethodAttribute}))));
        }
    }
}
