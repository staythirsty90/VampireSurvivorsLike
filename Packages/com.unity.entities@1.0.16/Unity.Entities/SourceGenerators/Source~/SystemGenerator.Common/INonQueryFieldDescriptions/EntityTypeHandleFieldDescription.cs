using System;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct EntityTypeHandleFieldDescription : INonQueryFieldDescription, IEquatable<EntityTypeHandleFieldDescription>
    {
        public string GeneratedFieldName => "__Unity_Entities_Entity_TypeHandle";
        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"[global::Unity.Collections.ReadOnly] {(forcePublic ? "public" : "")} global::Unity.Entities.EntityTypeHandle {GeneratedFieldName};";
        public string GetFieldAssignment() => $@"{GeneratedFieldName} = state.GetEntityTypeHandle();";
        public bool Equals(EntityTypeHandleFieldDescription other) => true;
        public override int GetHashCode() => GeneratedFieldName.GetHashCode();
    }
}
