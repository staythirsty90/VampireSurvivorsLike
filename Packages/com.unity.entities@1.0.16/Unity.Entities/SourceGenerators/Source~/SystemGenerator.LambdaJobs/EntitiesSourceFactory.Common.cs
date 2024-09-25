using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;
using Unity.Entities.SourceGen.Common;

namespace Unity.Entities.SourceGen.LambdaJobs
{
    public static partial class EntitiesSourceFactory
    {
        public static class Common
        {
            public static string NoAliasAttribute(LambdaJobDescription description) =>
                description.Burst.IsEnabled ? @"[global::Unity.Burst.NoAlias]" : string.Empty;

            public static string BurstCompileAttribute(LambdaJobDescription description)
            {
                if (description.Burst.IsEnabled)
                {
                    var parameters = new List<string>();
                    if (description.Burst.Settings.BurstFloatMode != null)
                        parameters.Add($"FloatMode=global::Unity.Burst.FloatMode.{description.Burst.Settings.BurstFloatMode}");
                    if (description.Burst.Settings.BurstFloatPrecision != null)
                        parameters.Add($"FloatPrecision=global::Unity.Burst.FloatPrecision.{description.Burst.Settings.BurstFloatPrecision}");
                    if (description.Burst.Settings.SynchronousCompilation != null)
                        parameters.Add($"CompileSynchronously={description.Burst.Settings.SynchronousCompilation.ToString().ToLower()}");

                    return parameters.Count == 0
                        ? "[global::Unity.Burst.BurstCompile]"
                        : $"[global::Unity.Burst.BurstCompile({string.Join(", ", parameters)})]";
                }
                else
                    return string.Empty;
            }

            public static string MonoPInvokeCallbackAttributeAttribute(LambdaJobDescription description) =>
                description.LambdaJobKind == LambdaJobKind.Entities ?
                        $@"[global::AOT.MonoPInvokeCallback(typeof(global::Unity.Entities.Internal.InternalCompilerInterface.JobChunkRunWithoutJobSystemDelegate))]" :
                        $@"[global::AOT.MonoPInvokeCallback(typeof(global::Unity.Entities.Internal.InternalCompilerInterface.JobRunWithoutJobSystemDelegate))]";

            public static SyntaxNode SchedulingInvocationFor(LambdaJobDescription description)
            {
                static string ExecuteMethodArgs(LambdaJobDescription description)
                {
                    var argStrings = new HashSet<string>();
                    foreach (var variable in description.VariablesCaptured)
                    {
                        if (!variable.IsThis)
                            argStrings.Add(description.Schedule.Mode == ScheduleMode.Run && variable.IsWritable
                                ? $"ref {variable.OriginalVariableName}"
                                : variable.OriginalVariableName);
                    }

                    if (description.Schedule.DependencyArgument != null)
                        argStrings.Add($@"{description.Schedule.DependencyArgument.ToString()}");

                    if (description.WithFilterEntityArray != null)
                        argStrings.Add($@"{description.WithFilterEntityArray.ToString()}");

                    foreach (var argument in description.AdditionalVariablesCapturedForScheduling)
                        argStrings.Add(argument.Name);

                    return argStrings.SeparateByComma();
                }

                var template = $@"{description.ExecuteInSystemMethodName}({ExecuteMethodArgs(description)}));";
                return SyntaxFactory.ParseStatement(template).DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            }

            public static string SharedComponentFilterInvocations(LambdaJobDescription description)
            {
                if (!description.HasSharedComponentFilter)
                    return string.Empty;

                return
                    description
                        .WithSharedComponentFilterArgumentSyntaxes
                        .Select(arg => $@"{description.EntityQueryFieldName}.SetSharedComponentFilter({arg});")
                        .SeparateByNewLine();
            }

            public static string ResetSharedComponentFilter(LambdaJobDescription description) =>
                !description.HasSharedComponentFilter ? string.Empty : $@"{description.EntityQueryFieldName}.ResetFilter();";
        }
    }
}
