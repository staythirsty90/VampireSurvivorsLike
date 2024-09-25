using System;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct AspectLookupFieldDescription : IEquatable<AspectLookupFieldDescription>, INonQueryFieldDescription
    {
        ITypeSymbol TypeSymbol { get; }
        bool IsReadOnly { get; }
        public string GeneratedFieldName { get; }

        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"{"[global::Unity.Collections.ReadOnly] ".EmitIfTrue(IsReadOnly)} {(forcePublic ? "public" : "")} {TypeSymbol.ToFullName()}.Lookup {GeneratedFieldName};";
        public string GetFieldAssignment() =>
            $@"{GeneratedFieldName} = new {TypeSymbol.ToFullName()}.Lookup(ref state);";

        public AspectLookupFieldDescription(ITypeSymbol typeSymbol, bool isReadOnly)
        {
            TypeSymbol = typeSymbol;
            IsReadOnly = isReadOnly;

            GeneratedFieldName = $"__{TypeSymbol.ToValidIdentifier()}_{(IsReadOnly ? "RO" : "RW")}_AspectLookup";
        }

        public bool Equals(AspectLookupFieldDescription other) =>
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
