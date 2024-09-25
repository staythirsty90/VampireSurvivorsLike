using System;

namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public readonly struct ContainerTypeHandleFieldDescription : IEquatable<ContainerTypeHandleFieldDescription>, INonQueryFieldDescription
    {
        string ContainerTypeName { get; }
        public string GeneratedFieldName { get; }

        public string GetFieldDeclaration(bool forcePublic = false) =>
            $"{(forcePublic ? "public" : "")} {ContainerTypeName}.TypeHandle {GeneratedFieldName};";
        public string GetFieldAssignment()
            => $@"{GeneratedFieldName} = new {ContainerTypeName}.TypeHandle(ref state, isReadOnly: false);";

        public ContainerTypeHandleFieldDescription(string containerTypeName)
        {
            ContainerTypeName = containerTypeName;
            GeneratedFieldName = $"__{containerTypeName.Replace(".", "_")}_RW_TypeHandle";
        }

        public bool Equals(ContainerTypeHandleFieldDescription other) => ContainerTypeName == other.ContainerTypeName;
        public override int GetHashCode() => ContainerTypeName != null ? ContainerTypeName.GetHashCode() : 0;
    }
}
