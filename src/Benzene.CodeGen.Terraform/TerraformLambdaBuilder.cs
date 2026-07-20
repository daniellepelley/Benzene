using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;

namespace Benzene.CodeGen.Terraform;

public class TerraformLambdaBuilder : ICodeBuilder<TerraformLambdaSettings>
{
    public ICodeFile[] BuildCodeFiles(TerraformLambdaSettings settings)
    {
        var list = new List<ICodeFile>
        {
            new CodeFile("lambda.tf", BuildLambda(settings)),
            new CodeFile("iam_roles.tf", BuildRole(settings))
        };

        if (settings.TopicsMap != null)
        {
            var b = new TerraformLambdaEventBusPermissionsBuilder();
            var permissionsCodeFiles = b.BuildCodeFiles(new TerraformLambdaEventBusPermissionsSettings
            {
                LambdaName = settings.Name, TopicsMap = settings.TopicsMap
            });
            list.AddRange(permissionsCodeFiles);
        }

        if (settings.EventBridge != null)
        {
            settings.EventBridge.LambdaName ??= settings.Name;
            list.AddRange(new TerraformEventBridgeRuleBuilder().BuildCodeFiles(settings.EventBridge));
        }

        return list.ToArray();
    }

    public string[] BuildLambda(TerraformLambdaSettings settings)
    {
        var lambdaName = GetLambdaName(settings.Name);

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_lambda_function\" \"{lambdaName}\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine($"function_name = \"{settings.Name}\"");
            lineWriter.WriteLine("filename = \"${path.module}/file.zip\"");
            lineWriter.WriteLine($"role = aws_iam_role.{lambdaName}_role.arn");
            lineWriter.WriteLine($"handler = \"{settings.EntryPoint}\"");
            lineWriter.WriteLine($"runtime = \"{settings.Runtime}\"");
            lineWriter.WriteLine($"timeout = {settings.Timeout}");
            lineWriter.WriteLine($"memory_size = {settings.MemorySize}");

            if (settings.ReservedConcurrentExecutions > 0)
            {
                lineWriter.WriteLine($"reserved_concurrent_executions = {settings.ReservedConcurrentExecutions}");
            }

            if (settings.SubnetIdsExpression != null)
            {
                lineWriter.WriteLine();
                lineWriter.WriteLine("vpc_config {");
                using (lineWriter.StartIndent())
                {
                    lineWriter.WriteLine("security_group_ids = [");
                    lineWriter.WriteLine("]");
                    lineWriter.WriteLine($"subnet_ids = {settings.SubnetIdsExpression}");
                }

                lineWriter.WriteLine("}");
            }

            lineWriter.WriteLine();
            lineWriter.WriteLine("tracing_config {");
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine("mode = local.tracing_config");
            }

            lineWriter.WriteLine("}");
            lineWriter.WriteLine();
            lineWriter.WriteLine("lifecycle {");
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine("ignore_changes = [");
                using (lineWriter.StartIndent())
                {
                    var ignored = settings.IgnoredChanges.ToArray();
                    for (var i = 0; i < ignored.Length; i++)
                    {
                        var comma = i < ignored.Length - 1 ? "," : string.Empty;
                        lineWriter.WriteLine($"{ignored[i]}{comma}");
                    }
                }
                lineWriter.WriteLine("]");
            }

            lineWriter.WriteLine("}");
            lineWriter.WriteLine();
            lineWriter.WriteLine("tags = {");
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine($"name = \"{settings.Name}\"");
                lineWriter.WriteLine($"domain = \"{settings.Domain}\"");
                lineWriter.WriteLine($"subdomain = \"{settings.SubDomain}\"");
            }

            lineWriter.WriteLine("}");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    public string[] BuildRole(TerraformLambdaSettings settings)
    {
        var lambdaName = GetLambdaName(settings.Name);

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_iam_role\" \"{lambdaName}_role\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine($"name = \"{settings.Name}-role\"");
            lineWriter.WriteLine("assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json");
            lineWriter.WriteLine();
            lineWriter.WriteLine("tags = {");
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine($"name = \"{settings.Name}-role\"");
                lineWriter.WriteLine($"domain = \"{settings.Domain}\"");
                lineWriter.WriteLine($"subdomain = \"{settings.SubDomain}\"");
            }
            lineWriter.WriteLine("}");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    private static string GetLambdaName(string name)
    {
        return name.Replace("-", "_");
    }
}
