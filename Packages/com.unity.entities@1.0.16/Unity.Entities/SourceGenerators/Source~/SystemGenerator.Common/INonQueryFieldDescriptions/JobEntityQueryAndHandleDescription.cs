using System;
using Microsoft.CodeAnalysis;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct JobEntityQueryAndHandleDescription : IEquatable<JobEntityQueryAndHandleDescription>, INonQueryFieldDescription
    {
        ITypeSymbol TypeSymbol { get; }
        bool AssignDefaultQuery { get; }
        public string GeneratedFieldName{ get; }

        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"{(forcePublic ? "public" : "")} {TypeSymbol.ToFullName()}.InternalCompilerQueryAndHandleData {GeneratedFieldName};";
        public string GetFieldAssignment() =>
            $@"{GeneratedFieldName}.Init(ref state, {(AssignDefaultQuery ? "true" : "false")});";

        public JobEntityQueryAndHandleDescription(ITypeSymbol typeSymbol, bool assignDefaultQuery)
        {
            TypeSymbol = typeSymbol;
            AssignDefaultQuery = assignDefaultQuery;

            GeneratedFieldName = $"__{TypeSymbol.ToValidIdentifier()}_{(AssignDefaultQuery ? "With" : "Without")}DefaultQuery_JobEntityTypeHandle";
        }

        public bool Equals(JobEntityQueryAndHandleDescription other) =>
            SymbolEqualityComparer.Default.Equals(TypeSymbol, other.TypeSymbol) && AssignDefaultQuery == other.AssignDefaultQuery;

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TypeSymbol != null ?
                    SymbolEqualityComparer.Default.GetHashCode(TypeSymbol) : 0) * 397) ^ AssignDefaultQuery.GetHashCode();
            }
        }
    }
}
