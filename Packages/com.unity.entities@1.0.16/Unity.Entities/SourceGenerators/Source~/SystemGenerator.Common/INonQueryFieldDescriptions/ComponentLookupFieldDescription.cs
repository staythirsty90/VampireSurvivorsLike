using System;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct ComponentLookupFieldDescription : IEquatable<ComponentLookupFieldDescription>, INonQueryFieldDescription
    {
        ITypeSymbol TypeSymbol { get; }
        bool IsReadOnly { get; }
        public string GeneratedFieldName{ get; }

        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"{"[global::Unity.Collections.ReadOnly] ".EmitIfTrue(IsReadOnly)} {(forcePublic ? "public" : "")} Unity.Entities.ComponentLookup<{TypeSymbol.ToFullName()}> {GeneratedFieldName};";
        public string GetFieldAssignment() =>
            $@"{GeneratedFieldName} = state.GetComponentLookup<{TypeSymbol.ToFullName()}>({(IsReadOnly ? "true" : "false")});";

        public ComponentLookupFieldDescription(ITypeSymbol typeSymbol, bool isReadOnly)
        {
            TypeSymbol = typeSymbol;
            IsReadOnly = isReadOnly;

            GeneratedFieldName = $"__{TypeSymbol.ToValidIdentifier()}_{(IsReadOnly ? "RO" : "RW")}_ComponentLookup";
        }

        public bool Equals(ComponentLookupFieldDescription other) =>
            SymbolEqualityComparer.Default.Equals(TypeSymbol, other.TypeSymbol) && IsReadOnly == other.IsReadOnly;

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TypeSymbol != null ?
                    SymbolEqualityComparer.Default.GetHashCode(TypeSymbol) : 0) * 397) ^ IsReadOnly.GetHashCode();
            }
        }
    }
}
