using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Unity.Entities.SourceGen.SystemGenerator.SystemAPI.QueryBuilder;
using VerifyCS =
    Unity.Entities.SourceGenerators.Test.CSharpSourceGeneratorVerifier<
        Unity.Entities.SourceGen.SystemGenerator.SystemGenerator>;

namespace Unity.Entities.SourceGenerators
{
    [TestClass]
    public class SystemAPIQueryBuilderErrorTests
    {
        [TestMethod]
        public async Task SGQB001_MultipleWithOptionsInvocations()
        {
            const string source = @"
                using Unity.Entities;
                using Unity.Entities.Tests;
                partial class SomeSystem : SystemBase
                {
                    protected override void OnUpdate()
                    {
                        var query = {|#0:SystemAPI.QueryBuilder().WithAll<EcsTestData>().WithOptions(EntityQueryOptions.IncludePrefab).WithOptions(EntityQueryOptions.IncludeDisabledEntities).Build()|};
                        EntityManager.AddComponent<EcsTestTag>(query);
                    }
                }";
            var expected = VerifyCS.CompilerError(nameof(SystemAPIQueryBuilderErrors.SGQB001)).WithLocation(0);
            await VerifyCS.VerifySourceGeneratorAsync(source, expected);
        }
    }
}
