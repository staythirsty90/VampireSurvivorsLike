using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

using VerifyCS = Unity.Entities.Analyzer.Test.CSharpCodeFixVerifier<
    Unity.Entities.Analyzer.TypeAnalyzer,
    Unity.Entities.Analyzer.EntitiesCodeFixProvider>;

namespace Unity.Entities.Analyzer
{
    [TestClass]
    public class TypeTests
    {
        [TestMethod]
        public async Task SystemBase()
        {
            const string test = @"
                using Unity.Entities;
                class {|#0:TestSystem|} : SystemBase
                {
                    protected override void OnUpdate(){}
                }";
            const string fixedSource = @"
                using Unity.Entities;
                partial class TestSystem : SystemBase
                {
                    protected override void OnUpdate(){}
                }";
            var expected = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0007Descriptor).WithLocation(0).WithArguments("SystemBase", "global::TestSystem");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task ISystem()
        {
            const string test = @"
                using Unity.Entities;
                struct {|#0:TestSystem|} : ISystem{}";
            const string fixedSource = @"
                using Unity.Entities;
                partial struct TestSystem : ISystem{}";
            var expected = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0007Descriptor).WithLocation(0).WithArguments("ISystem", "global::TestSystem");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task Aspect()
        {
            var test = @"
                using Unity.Entities;
                struct {|#0:TestAspect|} : IAspect {}";
            var fixedSource = @"
                using Unity.Entities;
                partial struct TestAspect : IAspect {}";
            var expected = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0007Descriptor).WithLocation(0).WithArguments("IAspect", "global::TestAspect");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task AspectParent()
        {
            var test = @"
                using Unity.Entities;
                struct {|#0:A|} {
                    struct {|#1:B|} {
                        partial struct TestAspect : IAspect {}
                    }
                }";
            var fixedSource = @"
                using Unity.Entities;
                partial struct A {
                    partial struct B {
                        partial struct TestAspect : IAspect {}
                    }
                }";
            var expectedA = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0008Descriptor).WithLocation(0).WithArguments("IAspect", "global::A.B.TestAspect", "global::A");
            var expectedB = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0008Descriptor).WithLocation(1).WithArguments("IAspect", "global::A.B.TestAspect", "global::A.B");
            await VerifyCS.VerifyCodeFixAsync(test, new[]{expectedA, expectedB}, fixedSource);
        }

        [TestMethod]
        public async Task JobEntity()
        {
            var test = @"
                using Unity.Entities;
                struct {|#0:TestJob|} : IJobEntity {}";
            var fixedSource = @"
                using Unity.Entities;
                partial struct TestJob : IJobEntity {}";
            var expected = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0007Descriptor).WithLocation(0).WithArguments("IJobEntity", "global::TestJob");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        [TestMethod]
        public async Task JobEntityParent()
        {
            var test = @"
                using Unity.Entities;
                struct {|#0:A|} {
                    struct {|#1:B|} {
                        partial struct TestJob : IJobEntity {}
                    }
                }";
            var fixedSource = @"
                using Unity.Entities;
                partial struct A {
                    partial struct B {
                        partial struct TestJob : IJobEntity {}
                    }
                }";
            var expectedA = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0008Descriptor).WithLocation(0).WithArguments("IJobEntity", "global::A.B.TestJob", "global::A");
            var expectedB = VerifyCS.Diagnostic(EntitiesDiagnostics.k_Ea0008Descriptor).WithLocation(1).WithArguments("IJobEntity", "global::A.B.TestJob", "global::A.B");
            await VerifyCS.VerifyCodeFixAsync(test,new[]{expectedA, expectedB}, fixedSource);
        }

        [TestMethod]
        public async Task DisableWarn()
        {
            var test = @"
                using Unity.Entities;
                #pragma warning disable EA0007
                struct TestJob : IJobEntity {}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
