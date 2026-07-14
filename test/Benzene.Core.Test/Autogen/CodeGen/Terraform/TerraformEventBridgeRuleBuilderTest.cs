using System;
using System.Linq;
using Benzene.CodeGen.Terraform;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Terraform;

public class TerraformEventBridgeRuleBuilderTest
{
    private static TerraformEventBridgeRuleSettings CreateSettings() => new TerraformEventBridgeRuleSettings
    {
        LambdaName = "benzene-orders-func",
        Topics = new[] { "order.created", "order.updated" }
    };

    [Fact]
    public void BuildRule_MatchesDetailTypesVerbatim()
    {
        var result = new TerraformEventBridgeRuleBuilder().BuildRule(CreateSettings());

        Assert.Equal(new[]
        {
            "resource \"aws_cloudwatch_event_rule\" \"benzene_orders_func_event_rule\" {",
            "  name = \"benzene-orders-func-event-rule\"",
            "  event_pattern = jsonencode({",
            "    \"detail-type\" = [\"order.created\", \"order.updated\"]",
            "  })",
            "}"
        }, result);
    }

    [Fact]
    public void BuildRule_WithEventBusNameAndSources()
    {
        var settings = CreateSettings();
        settings.EventBusName = "orders-bus";
        settings.Sources = new[] { "com.example.orders" };

        var result = new TerraformEventBridgeRuleBuilder().BuildRule(settings);

        Assert.Equal(new[]
        {
            "resource \"aws_cloudwatch_event_rule\" \"benzene_orders_func_event_rule\" {",
            "  name = \"benzene-orders-func-event-rule\"",
            "  event_bus_name = \"orders-bus\"",
            "  event_pattern = jsonencode({",
            "    \"detail-type\" = [\"order.created\", \"order.updated\"]",
            "    \"source\" = [\"com.example.orders\"]",
            "  })",
            "}"
        }, result);
    }

    [Fact]
    public void BuildTarget_ReferencesRuleAndLambda()
    {
        var result = new TerraformEventBridgeRuleBuilder().BuildTarget(CreateSettings());

        Assert.Equal(new[]
        {
            "resource \"aws_cloudwatch_event_target\" \"benzene_orders_func_event_target\" {",
            "  rule = aws_cloudwatch_event_rule.benzene_orders_func_event_rule.name",
            "  arn = aws_lambda_function.benzene_orders_func.arn",
            "}"
        }, result);
    }

    [Fact]
    public void BuildTarget_WithEventBusName_SetsBusOnTarget()
    {
        var settings = CreateSettings();
        settings.EventBusName = "orders-bus";

        var result = new TerraformEventBridgeRuleBuilder().BuildTarget(settings);

        Assert.Contains("  event_bus_name = \"orders-bus\"", result);
    }

    [Fact]
    public void BuildPermission_AllowsEventBridgeToInvokeLambda()
    {
        var result = new TerraformEventBridgeRuleBuilder().BuildPermission(CreateSettings());

        Assert.Equal(new[]
        {
            "resource \"aws_lambda_permission\" \"eventbridge_invoke_benzene_orders_func\" {",
            "  action = \"lambda:InvokeFunction\"",
            "  function_name = aws_lambda_function.benzene_orders_func.function_name",
            "  principal = \"events.amazonaws.com\"",
            "  statement_id = \"AllowEventBridgeInvoke\"",
            "  source_arn = aws_cloudwatch_event_rule.benzene_orders_func_event_rule.arn",
            "}"
        }, result);
    }

    [Fact]
    public void BuildCodeFiles_GeneratesRuleTargetAndPermissionFiles()
    {
        var result = new TerraformEventBridgeRuleBuilder().BuildCodeFiles(CreateSettings());

        Assert.Equal(new[]
        {
            "aws_cloudwatch_event_rule.tf",
            "aws_cloudwatch_event_target.tf",
            "aws_lambda_permission_eventbridge.tf"
        }, result.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void BuildCodeFiles_WithoutTopics_Throws()
    {
        var settings = CreateSettings();
        settings.Topics = Array.Empty<string>();

        Assert.Throws<ArgumentException>(() => new TerraformEventBridgeRuleBuilder().BuildCodeFiles(settings));
    }

    [Fact]
    public void BuildCodeFiles_FromMessageHandlerTypes_DiscoversDistinctSortedTopics()
    {
        var settings = new TerraformEventBridgeRuleSettings { LambdaName = "benzene-orders-func" };

        // ExampleMessageHandler and ExampleMessageHandlerV2 share a topic (different versions) —
        // versions never reach the wire, so the topic must appear once.
        var result = new TerraformEventBridgeRuleBuilder().BuildCodeFiles(settings,
            typeof(ExampleMessageHandlerV2), typeof(ExampleNoResponseMessageHandler), typeof(ExampleMessageHandler));

        Assert.Equal(new[] { Defaults.Topic, Defaults.TopicNoResponse }, settings.Topics);

        var rule = result.Single(x => x.Name == "aws_cloudwatch_event_rule.tf");
        Assert.Contains($"    \"detail-type\" = [\"{Defaults.Topic}\", \"{Defaults.TopicNoResponse}\"]", rule.Lines);
    }

    [Fact]
    public void TerraformLambdaBuilder_WithEventBridgeSettings_AppendsRuleFilesAndDefaultsLambdaName()
    {
        var eventBridgeSettings = new TerraformEventBridgeRuleSettings
        {
            Topics = new[] { "order.created" }
        };

        var result = new TerraformLambdaBuilder().BuildCodeFiles(new TerraformLambdaSettings
        {
            Name = "benzene-orders-func",
            EntryPoint = "Benzene.Orders.Func::Benzene.Orders.LambdaEntryPoint::FunctionHandlerAsync",
            Domain = "benzene",
            SubDomain = "orders",
            EventBridge = eventBridgeSettings
        });

        Assert.Equal("benzene-orders-func", eventBridgeSettings.LambdaName);
        Assert.Equal(new[]
        {
            "lambda.tf",
            "iam_roles.tf",
            "aws_cloudwatch_event_rule.tf",
            "aws_cloudwatch_event_target.tf",
            "aws_lambda_permission_eventbridge.tf"
        }, result.Select(x => x.Name).ToArray());
    }
}
