# Terraform Code Generation

Benzene includes tools to automatically generate Terraform configuration for your AWS Lambda functions, ensuring that your infrastructure stays in sync with your code.

## Setup

The Terraform generation logic is contained in the `Benzene.CodeGen.Terraform` package.

## Usage

You can use the `TerraformLambdaBuilder` to generate `.tf` files based on your service settings.

```csharp
var terraformBuilder = new TerraformLambdaBuilder();

var result = terraformBuilder.Build(new TerraformLambdaSettings
{
    Name = "my-service-func",
    EntryPoint = "MyNamespace::MyNamespace.LambdaEntryPoint::FunctionHandlerAsync",
    Timeout = 30,
    MemorySize = 2048,
    Domain = "my-domain",
    SubDomain = "my-subdomain"
});

// result is a dictionary of filename -> content
var lambdaTf = result["lambda.tf"];
var rolesTf = result["iam_roles.tf"];
```

## Generated Resources

The tool typically generates:

- `aws_lambda_function`: The Lambda function itself, with configured runtime, handler, and VPC settings.
- `aws_iam_role` & `aws_iam_role_policy`: The execution role and permissions for the Lambda.
- `aws_lambda_permission`: Permissions for external services (like SNS) to invoke the Lambda.
- `aws_sns_topic_subscription`: Subscriptions if the Lambda is triggered by SNS.

## Customization

The generated Terraform code follows Benzene's naming conventions and best practices for serverless deployments on AWS. You can customize the settings passed to the builder to adjust memory, timeouts, and other parameters.
