using System.Linq;
using Benzene.CodeGen.Terraform;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Terraform;

public class TerraformLambdaEventBusPermissionsBuilderTest
{
    private static TerraformLambdaEventBusPermissionsSettings CreateSettings() => new TerraformLambdaEventBusPermissionsSettings
    {
        LambdaName = "benzene-orders-func",
        TopicsMap = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["orders-topic"] = new[] { "order.created", "order.updated" }
        }
    };

    [Fact]
    public void BuildPermission_AllowsSnsToInvokeLambda()
    {
        var result = new TerraformLambdaEventBusPermissionsBuilder().BuildPermission("benzene-orders-func", "orders-topic");

        Assert.Equal(new[]
        {
            "resource \"aws_lambda_permission\" \"orders_topic_invoke_benzene_orders_func\" {",
            "  action = \"lambda:InvokeFunction\"",
            "  function_name = aws_lambda_function.benzene_orders_func.function_name",
            "  principal = \"sns.amazonaws.com\"",
            "  statement_id = \"AllowSubscriptionToSNSResponse\"",
            "  source_arn = data.terraform_remote_state.sns.outputs.orders_topic",
            "}"
        }, result);
    }

    [Fact]
    public void BuildSubscription_FiltersOnTheMessageTopics()
    {
        var result = new TerraformLambdaEventBusPermissionsBuilder()
            .BuildSubscription("benzene-orders-func", "orders-topic", new[] { "order.created", "order.updated" });

        Assert.Equal(new[]
        {
            "resource \"aws_sns_topic_subscription\" \"benzene_orders_func_orders_topic_subscription\" {",
            "  topic_arn = data.terraform_remote_state.sns.outputs.orders_topic",
            "  protocol = \"lambda\"",
            "  endpoint = aws_lambda_function.benzene_orders_func.arn",
            "  endpoint_auto_confirms = true",
            "  filter_policy = jsonencode({\"topic\" = [\"order.created\",\"order.updated\"]})",
            "}"
        }, result);
    }

    [Fact]
    public void BuildPermissions_OneEntryPerTopicInTheMap()
    {
        var settings = CreateSettings();
        settings.TopicsMap = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["orders-topic"] = new[] { "order.created" },
            ["shipping-topic"] = new[] { "shipment.dispatched" }
        };

        var result = new TerraformLambdaEventBusPermissionsBuilder().BuildPermissions(settings);

        Assert.Equal(2, result.Count(line => line.StartsWith("resource \"aws_lambda_permission\"")));
        Assert.Contains("resource \"aws_lambda_permission\" \"orders_topic_invoke_benzene_orders_func\" {", result);
        Assert.Contains("resource \"aws_lambda_permission\" \"shipping_topic_invoke_benzene_orders_func\" {", result);
    }

    [Fact]
    public void BuildSubscriptions_OneEntryPerTopicInTheMap()
    {
        var settings = CreateSettings();
        settings.TopicsMap = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["orders-topic"] = new[] { "order.created" },
            ["shipping-topic"] = new[] { "shipment.dispatched" }
        };

        var result = new TerraformLambdaEventBusPermissionsBuilder().BuildSubscriptions(settings);

        Assert.Equal(2, result.Count(line => line.StartsWith("resource \"aws_sns_topic_subscription\"")));
    }

    [Fact]
    public void BuildCodeFiles_GeneratesPermissionAndSubscriptionFiles()
    {
        var result = new TerraformLambdaEventBusPermissionsBuilder().BuildCodeFiles(CreateSettings());

        Assert.Equal(new[]
        {
            "aws_lambda_permission.tf",
            "aws_sns_topic_subscription.tf"
        }, result.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void NameFormatter_UnderScoreCase_ReplacesHyphensWithUnderscores()
    {
        Assert.Equal("benzene_orders_func", NameFormatter.UnderScoreCase("benzene-orders-func"));
    }

    [Fact]
    public void CustomSnsRemoteStateName_IsUsedInTheArnReference()
    {
        var result = new TerraformLambdaEventBusPermissionsBuilder()
            .BuildPermission("benzene-orders-func", "orders-topic", "event_bus");

        Assert.Contains("  source_arn = data.terraform_remote_state.event_bus.outputs.orders_topic", result);
    }
}
