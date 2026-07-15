# BenzeneStarter

A [Benzene](https://github.com/daniellepelley/Benzene) service on AWS Lambda, triggered by SNS,
generated from the `benzene.aws.sns` template.

## Build and test locally

```bash
dotnet build
```

See [Testing Benzene](https://github.com/daniellepelley/Benzene/blob/main/docs/testing-benzene.md)
for `BenzeneTestHost` - a way to exercise `StartUp`/`Function` in-memory without deploying anything.

## Deploy

This template doesn't ship a SAM `template.yaml` - SNS invokes via a resource-based Lambda
permission tied to a specific topic ARN you'll supply, which is deployment-specific. See
[AWS Lambda Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md#sns)'s
SNS section for the permission shape - no extra execution-role IAM is needed to receive
notifications.

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it. The topic is resolved from a `topic` message attribute (or the SNS topic ARN,
  depending on delivery configuration), not the message body. SNS delivery is fire-and-forget - no
  response is written back, and the `[HttpEndpoint]` attribute on the starter handler is unused here
  but harmless to leave in place.
- **`StartUp.cs`** wires the AWS event source(s) this function handles - add
  `.UseApiGateway(...)`/`.UseSqs(...)`/`.UseKafka(...)` etc. alongside `.UseSns(...)` in `Configure`
  if this function should also handle other event sources in the same Lambda.
- Full guide: [AWS Lambda Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md)
