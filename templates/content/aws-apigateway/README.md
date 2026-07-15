# BenzeneStarter

A [Benzene](https://github.com/daniellepelley/Benzene) service on AWS Lambda, triggered by API
Gateway, generated from the `benzene.aws.apigateway` template.

## Build and test locally

```bash
dotnet build
```

See [Testing Benzene](https://github.com/daniellepelley/Benzene/blob/main/docs/testing-benzene.md)
for `BenzeneTestHost` - a way to exercise `StartUp`/`Function` in-memory without deploying anything.

## Deploy

Requires the [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html).
`template.yaml` is hand-checked against the pattern in the Benzene docs, but **not** validated or
deployed from this template - review it first.

```bash
sam build
sam deploy --guided
```

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it.
- **`StartUp.cs`** wires the AWS event source(s) this function handles - add `.UseSqs(...)`,
  `.UseSns(...)`, `.UseKafka(...)`, etc. alongside `.UseApiGateway(...)` in `Configure` if this
  function should also consume other event sources in the same Lambda.
- Full guide, including SQS/SNS/EventBridge/DynamoDB/S3/Kafka: [AWS Lambda Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md)
- Prefer a dedicated function per event source instead? `dotnet new benzene.aws.sqs` /
  `dotnet new benzene.aws.sns` start from the same handler, wired to a different trigger.
