# BenzeneStarter

A [Benzene](https://github.com/daniellepelley/Benzene) service on AWS Lambda, triggered by SQS,
generated from the `benzene.aws.sqs` template.

## Build and test locally

```bash
dotnet build
```

See [Testing Benzene](https://github.com/daniellepelley/Benzene/blob/main/docs/testing-benzene.md)
for `BenzeneTestHost` - a way to exercise `StartUp`/`Function` in-memory without deploying anything.

## Deploy

This template doesn't ship a SAM `template.yaml` - an SQS trigger needs a queue ARN you'll supply,
which is deployment-specific. See [AWS Lambda Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md#sqs)'s
SQS section for the event source mapping shape and the IAM permissions your execution role needs
(`sqs:ReceiveMessage`, `sqs:DeleteMessage`, `sqs:GetQueueAttributes` - see
[AWS IAM Permissions Reference](https://github.com/daniellepelley/Benzene/blob/main/docs/aws-iam-permissions.md)).

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it. Note the SQS message body is deserialized directly into the request type; the
  `[HttpEndpoint]` attribute on the starter handler is unused here (no HTTP concept on this
  transport) but harmless to leave in place.
- **`StartUp.cs`** wires the AWS event source(s) this function handles - add
  `.UseApiGateway(...)`/`.UseSns(...)`/`.UseKafka(...)` etc. alongside `.UseSqs(...)` in `Configure`
  if this function should also handle other event sources in the same Lambda.
- Full guide: [AWS Lambda Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-aws.md)
