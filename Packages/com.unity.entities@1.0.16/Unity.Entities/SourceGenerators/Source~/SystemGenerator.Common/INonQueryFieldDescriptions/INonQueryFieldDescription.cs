namespace Unity.Entities.SourceGen.SystemGenerator.Common
{
    public interface INonQueryFieldDescription
    {
        string GetFieldDeclaration(bool forcePublic = false);
        string GetFieldAssignment();
        string GeneratedFieldName { get; }
    }
}
