# AWS IAM Permissions Reference

This page lists the minimum IAM permissions each Benzene AWS package needs, along with
the specific AWS SDK call in Benzene's source that drives the requirement. Use it to
build a least-privilege execution role rather than reaching for a broad managed policy.

A couple of these are not obvious from AWS's own docs, so they're called out explicitly
below: several event sources (API Gateway, SNS, S3) invoke your Lambda function via a
**resource-based** permission on the function itself, not an identity-based IAM policy
on the execution role. Adding `sqs:*`-style permissions for those sources is a common
but unnecessary over-grant.

## Lambda execution role baseline

Every Lambda function needs this regardless of event source, so CloudWatch Logs
receives your function's logs:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:*:*:*"
    }
  ]
}
```

## API Gateway (`Benzene.Aws.Lambda.ApiGateway`)

**No execution-role IAM permissions are needed to receive requests.** API Gateway
invokes your function via a resource-based Lambda permission
(`AWS::Lambda::Permission` with `Principal: apigateway.amazonaws.com`), configured on
the function itself — not via the execution role's IAM policy. If you're using the
custom authorizer (`ApiGatewayCustomAuthorizerContext`), the same applies to the
authorizer function.

## SQS trigger (`Benzene.Aws.Lambda.Sqs`)

The Lambda event-source-mapping poller runs under your function's execution role and
needs:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes"
      ],
      "Resource": "arn:aws:sqs:REGION:ACCOUNT_ID:QUEUE_NAME"
    }
  ]
}
```

`sqs:DeleteMessage` covers both full-batch success and the partial-batch-failure
reporting `SqsApplication` supports (see `docs/getting-started-aws.md`).

## SNS trigger (`Benzene.Aws.Lambda.Sns`)

**No execution-role IAM permissions are needed to receive notifications.** Like API
Gateway, SNS invokes your function via a resource-based permission
(`Principal: sns.amazonaws.com`, scoped to the specific topic ARN as `SourceArn`), not
the execution role.

## S3 trigger (`Benzene.Aws.Lambda.S3`)

**No execution-role IAM permissions are needed to receive notifications.** S3 invokes
via a resource-based permission (`Principal: s3.amazonaws.com`) plus a bucket
notification configuration — again, not the execution role.

## Kafka trigger (`Benzene.Aws.Lambda.Kafka`)

> **Note:** unlike the sections above, these permissions are configured by AWS's event
> source mapping rather than driven by an explicit SDK call in Benzene's code, so this
> section is the least directly source-verified — cross-check against current AWS
> documentation before using it in production, especially if AWS has changed the
> required actions since this was written.

For an MSK cluster:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "kafka-cluster:Connect",
        "kafka-cluster:DescribeGroup",
        "kafka-cluster:AlterGroup",
        "kafka-cluster:DescribeTopic",
        "kafka-cluster:ReadData"
      ],
      "Resource": [
        "arn:aws:kafka:REGION:ACCOUNT_ID:cluster/CLUSTER_NAME/*",
        "arn:aws:kafka:REGION:ACCOUNT_ID:topic/CLUSTER_NAME/*",
        "arn:aws:kafka:REGION:ACCOUNT_ID:group/CLUSTER_NAME/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "ec2:CreateNetworkInterface",
        "ec2:DescribeNetworkInterfaces",
        "ec2:DeleteNetworkInterface",
        "ec2:DescribeVpcs",
        "ec2:DescribeSubnets",
        "ec2:DescribeSecurityGroups"
      ],
      "Resource": "*"
    }
  ]
}
```

The `ec2:*` block is needed because MSK event source mappings run your function inside
the cluster's VPC. For self-managed (non-MSK) Kafka, you'll additionally need
`secretsmanager:GetSecretValue` if you're using SASL/SCRAM authentication via a Secrets
Manager secret.

## `Benzene.Clients.Aws` outbound calls

These are the permissions your Lambda (or any host) needs when it *sends* messages via
the clients in `Benzene.Clients.Aws`, as opposed to receiving them:

| Client | Action | Source |
|---|---|---|
| `SqsBenzeneMessageClient` | `sqs:SendMessage` | `src/Benzene.Clients.Aws/Sqs/SqsClientMiddleware.cs` |
| `SnsBenzeneMessageClient` | `sns:Publish` | `src/Benzene.Clients.Aws/Sns/SnsClientMiddleware.cs` |
| `AwsLambdaBenzeneMessageClient` / `AwsLambdaClient` | `lambda:InvokeFunction` | `src/Benzene.Clients.Aws/Lambda/AwsLambdaClient.cs` |
| `StepFunctionsClient` | `states:StartExecution` | `src/Benzene.Clients.Aws/StepFunctions/StepFunctionsClient.cs` |

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": "sqs:SendMessage", "Resource": "arn:aws:sqs:REGION:ACCOUNT_ID:QUEUE_NAME" },
    { "Effect": "Allow", "Action": "sns:Publish", "Resource": "arn:aws:sns:REGION:ACCOUNT_ID:TOPIC_NAME" },
    { "Effect": "Allow", "Action": "lambda:InvokeFunction", "Resource": "arn:aws:lambda:REGION:ACCOUNT_ID:function:FUNCTION_NAME" },
    { "Effect": "Allow", "Action": "states:StartExecution", "Resource": "arn:aws:states:REGION:ACCOUNT_ID:stateMachine:STATE_MACHINE_NAME" }
  ]
}
```

Only include the statements for the clients you actually use.

## `Benzene.Aws.Sqs` standalone consumer

The polling worker (`SqsConsumer.StartAsync`, in `src/Benzene.Aws.Sqs/Consumer/SqsConsumer.cs`)
receives and deletes messages as a batch:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessageBatch",
        "sqs:GetQueueAttributes"
      ],
      "Resource": "arn:aws:sqs:REGION:ACCOUNT_ID:QUEUE_NAME"
    }
  ]
}
```

This is a separate package from the Lambda SQS trigger above — it's for a long-running
worker (e.g. behind `Benzene.SelfHost`/`Benzene.HostedService`) that polls SQS directly
rather than being invoked by a Lambda event source mapping.
