using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;

namespace Benzene.CodeGen.Terraform;

public static class NameFormatter
{
    public static string UnderScoreCase(string name)
    {
        return name.Replace("-", "_");
    }
}

public class TerraformLambdaEventBusPermissionsBuilder : ICodeBuilder<TerraformLambdaEventBusPermissionsSettings>
{
    public ICodeFile[] BuildCodeFiles(TerraformLambdaEventBusPermissionsSettings settings)
    {
        return new ICodeFile[]
        {
            new CodeFile("aws_lambda_permission.tf", BuildPermissions(settings)),
            new CodeFile("aws_sns_topic_subscription.tf", BuildSubscriptions(settings))
        };
    }

    public string[] BuildPermissions(TerraformLambdaEventBusPermissionsSettings settings)
    {
        var lineWriter = new LineWriter(2);

        foreach (var keyPairValue in settings.TopicsMap)
        {
            lineWriter.WriteLines(BuildPermission(settings.LambdaName, keyPairValue.Key, settings.SnsRemoteStateName));
        }

        return lineWriter.GetLines();
    }

    public string[] BuildPermission(string lambdaName, string snsTopic, string snsRemoteStateName = "sns")
    {
        var permissionName = $"{NameFormatter.UnderScoreCase(snsTopic)}_invoke_{NameFormatter.UnderScoreCase(lambdaName)}";

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_lambda_permission\" \"{permissionName}\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine("action = \"lambda:InvokeFunction\"");
            lineWriter.WriteLine($"function_name = aws_lambda_function.{NameFormatter.UnderScoreCase(lambdaName)}.function_name");
            lineWriter.WriteLine("principal = \"sns.amazonaws.com\"");
            lineWriter.WriteLine("statement_id = \"AllowSubscriptionToSNSResponse\"");
            lineWriter.WriteLine($"source_arn = data.terraform_remote_state.{snsRemoteStateName}.outputs.{NameFormatter.UnderScoreCase(snsTopic)}");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    public string[] BuildSubscriptions(TerraformLambdaEventBusPermissionsSettings settings)
    {
        var lineWriter = new LineWriter(2);

        foreach (var keyPairValue in settings.TopicsMap)
        {
            lineWriter.WriteLines(BuildSubscription(settings.LambdaName, keyPairValue.Key, keyPairValue.Value, settings.SnsRemoteStateName));
        }

        return lineWriter.GetLines();
    }

    public string[] BuildSubscription(string lambdaName, string snsTopic, string[] topics, string snsRemoteStateName = "sns")
    {
        var subscriptionName = $"{NameFormatter.UnderScoreCase(lambdaName)}_{NameFormatter.UnderScoreCase(snsTopic)}_subscription";

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_sns_topic_subscription\" \"{subscriptionName}\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine($"topic_arn = data.terraform_remote_state.{snsRemoteStateName}.outputs.{NameFormatter.UnderScoreCase(snsTopic)}");
            lineWriter.WriteLine("protocol = \"lambda\"");
            lineWriter.WriteLine($"endpoint = aws_lambda_function.{NameFormatter.UnderScoreCase(lambdaName)}.arn");
            lineWriter.WriteLine("endpoint_auto_confirms = true");
            lineWriter.WriteLine($"filter_policy = jsonencode({{\"topic\" = [{string.Join(",", topics.Select(topic => $"\"{topic}\""))}]}})");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }
}
