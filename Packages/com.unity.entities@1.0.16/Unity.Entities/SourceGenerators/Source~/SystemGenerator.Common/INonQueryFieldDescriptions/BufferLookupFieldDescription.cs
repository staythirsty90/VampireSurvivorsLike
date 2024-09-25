using System;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct BufferLookupFieldDescription : IEquatable<BufferLookupFieldDescription>, INonQueryFieldDescription
    {
        ITypeSymbol TypeSymbol { get; }
        bool IsReadOnly { get; }
        public string GeneratedFieldName { get; }

        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"{"[global::Unity.Collections.ReadOnly] ".EmitIfTrue(IsReadOnly)} {(forcePublic ? "public" : "")} Unity.Entities.BufferLookup<{TypeSymbol.ToFullName()}> {GeneratedFieldName};";
        public string GetFieldAssignment() =>
            $@"{GeneratedFieldName} = state.GetBufferLookup<{TypeSymbol.ToFullName()}>({(IsReadOnly ? "true" : "false")});";

        public BufferLookupFieldDescription(ITypeSymbol typeSymbol, bool isReadOnly)
        {
            TypeSymbol = typeSymbol;
            IsReadOnly = isReadOnly;

            GeneratedFieldName = $"__{TypeSymbol.ToValidIdentifier()}_{(IsReadOnly ? "RO" : "RW")}_BufferLookup";
        }

        public bool Equals(BufferLookupFieldDescription other) =>
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
