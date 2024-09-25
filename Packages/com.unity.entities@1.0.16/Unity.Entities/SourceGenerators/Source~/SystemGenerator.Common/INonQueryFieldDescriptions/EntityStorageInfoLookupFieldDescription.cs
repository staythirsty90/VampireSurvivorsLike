using System;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public struct EntityStorageInfoLookupFieldDescription : IEquatable<EntityStorageInfoLookupFieldDescription>, INonQueryFieldDescription
    {
        public string GeneratedFieldName => "__EntityStorageInfoLookup";
        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"[global::Unity.Collections.ReadOnly] {(forcePublic ? "public" : "")} Unity.Entities.EntityStorageInfoLookup {GeneratedFieldName};";
        public string GetFieldAssignment() =>
            $@"{GeneratedFieldName} = state.GetEntityStorageInfoLookup();";
        public bool Equals(EntityStorageInfoLookupFieldDescription other) => true;
        public override int GetHashCode() => 0;
    }
}
