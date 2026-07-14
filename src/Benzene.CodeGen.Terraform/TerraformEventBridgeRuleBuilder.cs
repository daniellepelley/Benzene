using Benzene.CodeGen.Core;
using Benzene.CodeGen.Core.Writers;

namespace Benzene.CodeGen.Terraform;

public class TerraformEventBridgeRuleBuilder : ICodeBuilder<TerraformEventBridgeRuleSettings>
{
    public ICodeFile[] BuildCodeFiles(TerraformEventBridgeRuleSettings settings)
    {
        if (settings.Topics == null || settings.Topics.Length == 0)
        {
            throw new ArgumentException("At least one topic is required to build an EventBridge rule", nameof(settings));
        }

        return new ICodeFile[]
        {
            new CodeFile("aws_cloudwatch_event_rule.tf", BuildRule(settings)),
            new CodeFile("aws_cloudwatch_event_target.tf", BuildTarget(settings)),
            new CodeFile("aws_lambda_permission_eventbridge.tf", BuildPermission(settings))
        };
    }

    public string[] BuildRule(TerraformEventBridgeRuleSettings settings)
    {
        var lambdaName = NameFormatter.UnderScoreCase(settings.LambdaName);

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_cloudwatch_event_rule\" \"{lambdaName}_event_rule\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine($"name = \"{settings.LambdaName}-event-rule\"");

            if (!string.IsNullOrEmpty(settings.EventBusName))
            {
                lineWriter.WriteLine($"event_bus_name = \"{settings.EventBusName}\"");
            }

            lineWriter.WriteLine("event_pattern = jsonencode({");
            using (lineWriter.StartIndent())
            {
                lineWriter.WriteLine($"\"detail-type\" = [{QuoteList(settings.Topics)}]");

                if (settings.Sources is { Length: > 0 })
                {
                    lineWriter.WriteLine($"\"source\" = [{QuoteList(settings.Sources)}]");
                }
            }

            lineWriter.WriteLine("})");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    public string[] BuildTarget(TerraformEventBridgeRuleSettings settings)
    {
        var lambdaName = NameFormatter.UnderScoreCase(settings.LambdaName);

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_cloudwatch_event_target\" \"{lambdaName}_event_target\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine($"rule = aws_cloudwatch_event_rule.{lambdaName}_event_rule.name");

            if (!string.IsNullOrEmpty(settings.EventBusName))
            {
                lineWriter.WriteLine($"event_bus_name = \"{settings.EventBusName}\"");
            }

            lineWriter.WriteLine($"arn = aws_lambda_function.{lambdaName}.arn");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    public string[] BuildPermission(TerraformEventBridgeRuleSettings settings)
    {
        var lambdaName = NameFormatter.UnderScoreCase(settings.LambdaName);

        var lineWriter = new LineWriter(2);
        lineWriter.WriteLine($"resource \"aws_lambda_permission\" \"eventbridge_invoke_{lambdaName}\" {{");
        using (lineWriter.StartIndent())
        {
            lineWriter.WriteLine("action = \"lambda:InvokeFunction\"");
            lineWriter.WriteLine($"function_name = aws_lambda_function.{lambdaName}.function_name");
            lineWriter.WriteLine("principal = \"events.amazonaws.com\"");
            lineWriter.WriteLine("statement_id = \"AllowEventBridgeInvoke\"");
            lineWriter.WriteLine($"source_arn = aws_cloudwatch_event_rule.{lambdaName}_event_rule.arn");
        }

        lineWriter.WriteLine("}");

        return lineWriter.GetLines();
    }

    private static string QuoteList(IEnumerable<string> values)
    {
        return string.Join(", ", values.Select(x => $"\"{x}\""));
    }
}
