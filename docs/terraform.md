# Terraform Code Generation

Benzene includes tools to automatically generate Terraform configuration for your AWS Lambda functions, ensuring that your infrastructure stays in sync with your code.

## Setup

The Terraform generation logic is contained in the `Benzene.CodeGen.Terraform` package.

## Usage

You can use the `TerraformLambdaBuilder` to generate `.tf` files based on your service settings.

```csharp
var terraformBuilder = new TerraformLambdaBuilder();

var codeFiles = terraformBuilder.BuildCodeFiles(new TerraformLambdaSettings
{
    Name = "my-service-func",
    EntryPoint = "MyNamespace::MyNamespace.StartUp::FunctionHandlerAsync",
    Timeout = 30,
    MemorySize = 2048,
    Domain = "my-domain",
    SubDomain = "my-subdomain"
});

// codeFiles is an ICodeFile[] — each has a Name (e.g. "lambda.tf") and Lines (the file content)
foreach (var file in codeFiles)
{
    File.WriteAllLines(file.Name, file.Lines);
}
```

## Generated Resources

The tool typically generates:

- `aws_lambda_function`: The Lambda function itself, with configured runtime, handler, and VPC settings.
- `aws_iam_role` & `aws_iam_role_policy`: The execution role and permissions for the Lambda.
- `aws_lambda_permission`: Permissions for external services (like SNS or EventBridge) to invoke the Lambda.
- `aws_sns_topic_subscription`: Subscriptions if the Lambda is triggered by SNS.
- `aws_cloudwatch_event_rule` & `aws_cloudwatch_event_target`: EventBridge routing if the Lambda consumes EventBridge events.

## EventBridge Rules

If your Lambda consumes EventBridge events (see [Getting Started with AWS](getting-started-aws.md)),
`TerraformEventBridgeRuleBuilder` generates the EventBridge-side wiring: one
`aws_cloudwatch_event_rule` whose `event_pattern` matches the `detail-type` of every message
topic the Lambda handles, an `aws_cloudwatch_event_target` pointing the rule at the Lambda, and
the `aws_lambda_permission` that lets EventBridge invoke it.

The topics can be listed explicitly, or — the interesting part — discovered from your
`[Message]` handlers, so the generated rule can never drift from what the code actually handles:

```csharp
var builder = new TerraformEventBridgeRuleBuilder();

var codeFiles = builder.BuildCodeFiles(new TerraformEventBridgeRuleSettings
{
    LambdaName = "my-service-func",
    EventBusName = "orders-bus",                    // optional — omit for the default bus
    Sources = new[] { "com.example.orders" }        // optional source filter
}, typeof(OrderCreatedHandler).Assembly);           // topics discovered from [Message] attributes
```

Handler versions are collapsed — every version of a topic shares one `detail-type` on the wire —
and topics are sorted so the output is diff-stable.

Alternatively, set `TerraformLambdaSettings.EventBridge` and `TerraformLambdaBuilder` generates
the rule files alongside the Lambda and IAM role (`LambdaName` defaults to the Lambda's `Name`):

```csharp
var codeFiles = terraformBuilder.BuildCodeFiles(new TerraformLambdaSettings
{
    Name = "my-service-func",
    EntryPoint = "MyNamespace::MyNamespace.StartUp::FunctionHandlerAsync",
    EventBridge = new TerraformEventBridgeRuleSettings
    {
        Topics = new[] { "order.created", "order.updated" }
    }
});
```

## Customization

The generated Terraform code follows Benzene's naming conventions and best practices for serverless deployments on AWS. You can customize the settings passed to the builder to adjust memory, timeouts, and other parameters.
