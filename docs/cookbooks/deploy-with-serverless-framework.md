# Deploying a Benzene Lambda with the Serverless Framework

Deploy a Benzene AWS Lambda using the [Serverless Framework](https://www.serverless.com/) (`serverless.yml`) instead of SAM, CDK, or Terraform — and keep the one thing that has to stay in sync between your infra config and your Benzene pipeline actually in sync.

## Problem Statement

Benzene is an application/runtime framework: it owns what runs *inside* your Lambda (the entry point, message routing, middleware, DI). It is deliberately agnostic about *how* the Lambda gets deployed — the `dotnet new` templates ship a SAM `template.yaml` for API Gateway and nothing for SQS/SNS, and the examples also demonstrate the `dotnet lambda` CLI and Terraform.

If your team has already standardized on the Serverless Framework across a polyglot estate (Node, Python, .NET) and you want your Benzene services to deploy through the same `serverless.yml` and CI pipeline as everything else, this cookbook shows you how. Because Benzene and the Serverless Framework sit on **different layers of the stack** (runtime vs. provisioning), there's no integration package to install — a Benzene Lambda is just a normal .NET Lambda zip with a handler string, and the Serverless Framework deploys it like any other.

The one seam worth understanding up front: Benzene lets a single Lambda accept **many** event sources, but only the ones you explicitly wire in code. Your `serverless.yml` `events:` list and your Benzene pipeline's `.UseXxx(...)` calls must agree — [see below](#the-one-seam-keep-events-and-usexxx-in-sync).

## Who this is for

- Teams already running the Serverless Framework and wanting .NET/Benzene services in the same pipeline.
- Anyone who prefers `serverless.yml`'s function-and-event model over hand-writing CloudFormation.

If you're not already invested in the Serverless Framework, the SAM path in [Getting Started with AWS](../getting-started-aws.md#7-deploy-with-sam) or the [Terraform generator](../terraform.md) (which derives infra *from* your `[Message]` handlers, eliminating the sync seam described here) are both first-class Benzene-documented options and worth comparing first.

> **Licensing note:** Serverless Framework **v4** moved to a paid model for organizations above a revenue threshold and requires a login/access key (`SERVERLESS_ACCESS_KEY`). **v3** is the last fully-open release. This cookbook's `serverless.yml` works on both; pick the version that fits your licensing situation.

## Prerequisites

- A Benzene AWS Lambda project — e.g. generated from the `benzene.aws.apigateway` template (`dotnet new benzene.aws.apigateway -n BenzeneStarter`). See [Project Templates](../getting-started-templates.md).
- [Node.js](https://nodejs.org/) and the Serverless Framework CLI: `npm install -g serverless`.
- The [.NET 10 SDK](https://dotnet.microsoft.com/download) and the AWS Lambda packaging tool: `dotnet tool install -g Amazon.Lambda.Tools`.
- AWS credentials configured (`aws configure`, or `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` env vars).

## How the two layers divide the work

This is the whole mental model, and it's why there's nothing to install:

| Concern | Owned by |
|---|---|
| Entry point, message routing, middleware, DI, serialization | **Benzene** (`AwsLambdaHost<StartUp>` + your `StartUp`) |
| Packaging the artifact, provisioning the function, wiring triggers, IAM, other resources | **Serverless Framework** (`serverless.yml`) |
| The **handler string** and **which event sources are enabled** | **Both** — they must agree |

The Benzene side is exactly what the template already generates. `Function.cs` is a one-liner:

```csharp
using Benzene.Aws.Lambda.Core;

namespace BenzeneStarter;

// AwsLambdaHost<StartUp> builds the pipeline once on cold start and implements the Lambda entry point.
public class Function : AwsLambdaHost<StartUp>
{
}
```

That gives Lambda a handler string of the form `Assembly::Namespace.Class::Method`:

```
BenzeneStarter::BenzeneStarter.Function::FunctionHandlerAsync
```

Everything else in this cookbook is the Serverless Framework side.

## Step-by-Step Implementation

### 1. Package the Benzene Lambda into a zip

The Serverless Framework doesn't build .NET for you — you build the deployment zip yourself and point `serverless.yml` at it. `Amazon.Lambda.Tools` produces a Lambda-shaped zip in one command:

```bash
dotnet lambda package \
  --configuration Release \
  --function-architecture arm64 \
  --output-package ./artifact.zip
```

This restores, publishes, and zips your project (respecting `aws-lambda-tools-defaults.json` if present). Use `arm64` to match the `template.yaml` the templates ship (cheaper and faster on Graviton); switch to `x86_64` if you prefer, but keep it consistent with `architecture:` in `serverless.yml` below.

> **Raw alternative (no Amazon.Lambda.Tools):** `dotnet publish -c Release -r linux-arm64 --self-contained false -o ./publish` then `cd publish && zip -r ../artifact.zip .`. The `dotnet lambda package` path is preferred because it gets the runtime/arch flags right for you.

### 2. Write `serverless.yml`

Drop this next to your `.csproj`. It deploys the same single Lambda the SAM template does, behind an HTTP API:

```yaml
service: benzene-starter

provider:
  name: aws
  runtime: dotnet8          # managed .NET 8 runtime; runs a net10.0 project fine (compatible Lambda ABI)
  architecture: arm64       # must match `dotnet lambda package --function-architecture`
  region: eu-west-1
  memorySize: 1024
  timeout: 30

package:
  artifact: artifact.zip    # the zip from step 1

functions:
  api:
    handler: BenzeneStarter::BenzeneStarter.Function::FunctionHandlerAsync
    events:
      - httpApi: '*'        # catch-all HTTP API → Benzene's ApiGateway router middleware
```

The `handler` string is the same `Assembly::Namespace.Class::Method` value from `template.yaml` — if you renamed the project from `BenzeneStarter`, rename all three segments to match.

### 3. Deploy

```bash
serverless deploy
```

The CLI packages nothing itself (you gave it `package.artifact`), synthesizes a CloudFormation stack, and deploys it. On success it prints the HTTP API endpoint. Hit it:

```bash
curl https://<api-id>.execute-api.eu-west-1.amazonaws.com/hello/world
# → {"message":"Hello world!"}
```

To tear it all down: `serverless remove`.

## The one seam: keep `events:` and `.UseXxx(...)` in sync

This is the only Benzene-specific gotcha, and it's the same drift risk any "code and infra live in separate files" setup has (SAM and hand-written Terraform included).

A single Benzene Lambda can accept several AWS event sources at once. Which ones it *actually* accepts is decided in `StartUp.Configure`, by the router middlewares you add under `UseAwsLambda`:

```csharp
public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
{
    app.UseAwsLambda(eventPipeline => eventPipeline
        .UseApiGateway(api => api
            .UseBenzeneEnrichment()
            .UseLogResult(_ => { })
            .UseMessageHandlers())
        .UseSqs(sqs => sqs                     // ← only if this Lambda should also handle SQS
            .UseMessageHandlers())
        .UseSns(sns => sns                     // ← only if this Lambda should also handle SNS
            .UseMessageHandlers()));
}
```

At runtime, each router (`ApiGatewayLambdaHandler`, `SqsLambdaHandler`, `SnsLambdaHandler`, …) inspects the incoming payload and claims it only if it matches. **If a payload arrives that no router claims, `AwsLambdaEntryPoint` throws `BenzeneException`.**

So the rule is: **every event source you wire in `serverless.yml` must have a matching `.UseXxx(...)` in `Configure`, and vice versa.**

A multi-source `serverless.yml` that matches the `Configure` above:

```yaml
functions:
  worker:
    handler: BenzeneStarter::BenzeneStarter.Function::FunctionHandlerAsync
    events:
      - httpApi: '*'                                    # ↔ .UseApiGateway(...)
      - sqs:
          arn: arn:aws:sqs:eu-west-1:123456789012:orders
      - sns:
          arn: arn:aws:sns:eu-west-1:123456789012:order-events
```

- Wire an `sqs` event but forget `.UseSqs(...)` → the Lambda is invoked, no router claims the `SQSEvent`, and it throws `BenzeneException`.
- Add `.UseSqs(...)` but no `sqs` event → the router is never exercised (harmless, just dead wiring).

> The `benzene.aws.sqs` and `benzene.aws.sns` templates ship **no** IaC precisely because an SQS/SNS trigger needs a queue/topic ARN you supply — `serverless.yml`'s `events:` block is a clean place to supply it. Reference an existing queue/topic by ARN as above, or declare one under `resources:` (see Variations).

## Local development

The Serverless Framework's `serverless-offline` plugin emulates API Gateway/Lambda for **Node** functions and does **not** run .NET handlers — don't rely on it for local Benzene testing. Use Benzene's own local paths instead:

- **`benzene.selfhost.http`** or **`benzene.asp`** — the same `HelloWorldMessageHandler` runs behind a local HTTP server with no AWS emulation at all (the point of "write your handlers once, host them anywhere"). Fastest inner loop.
- **`BenzeneTestHost` / `AwsLambdaBenzeneTestHost`** — drive the pipeline in-process from a test, no zip, no deploy. See [Integration Testing Lambda Functions](testing-lambda-functions.md).
- **`sam local invoke`** — if you want real Lambda-runtime emulation against the packaged zip.

## Testing

Deployment config isn't unit-testable, but the handler behind it is, and that's what matters — the same handler runs identically whether SAM, Terraform, or the Serverless Framework deployed it. Test the pipeline in-process with `AwsLambdaBenzeneTestHost` (build an event with `MessageBuilder.Create(topic, payload).AsApiGatewayRequest()` / `.AsSqs()` and assert on the result) as covered in [Integration Testing Lambda Functions](testing-lambda-functions.md).

To sanity-check the deployed function without curl-ing the URL:

```bash
serverless invoke --function api --path event.json
serverless logs --function api --tail            # stream CloudWatch logs
```

## Troubleshooting

### `BenzeneException` on invocation, but the handler is never entered

A payload reached the Lambda that no router claimed — the [`events:` ↔ `.UseXxx(...)` seam](#the-one-seam-keep-events-and-usexxx-in-sync). You wired an event source in `serverless.yml` that has no matching `.UseXxx(...)` in `StartUp.Configure`. Add the router (or remove the event).

### `Runtime.InvalidEntrypoint` / handler not found

The `handler:` string doesn't match your assembly/namespace/class. It's `Assembly::Namespace.Class::FunctionHandlerAsync`. If you renamed the project away from `BenzeneStarter`, all three segments change. The method (`FunctionHandlerAsync`) comes from `AwsLambdaHost<StartUp>` and never changes.

### Architecture mismatch (`exec format error` in the logs)

`architecture:` in `serverless.yml` and `--function-architecture` in `dotnet lambda package` disagree. Set both to `arm64` (or both to `x86_64`).

### `serverless deploy` uploads stale code

`package.artifact` points at a zip that wasn't rebuilt. Re-run `dotnet lambda package` before every `serverless deploy`, or wire it as an npm `predeploy`/CI step so the two never drift.

### The SQS trigger is configured but messages aren't retried per-message

That's a Benzene runtime concern (partial batch failure), not a Serverless Framework one — and it needs `functionResponseType: ReportBatchItemFailures` on the SQS event. See [Handling SQS Message Failures](handling-sqs-failures.md).

## Variations

### Declare the queue/topic in the same stack

Instead of referencing an existing ARN, let the Serverless Framework provision it under `resources:` (raw CloudFormation) and reference it with `Fn::GetAtt`:

```yaml
functions:
  worker:
    handler: BenzeneStarter::BenzeneStarter.Function::FunctionHandlerAsync
    events:
      - sqs:
          arn:
            Fn::GetAtt: [OrdersQueue, Arn]
          functionResponseType: ReportBatchItemFailures   # partial batch failures (see the SQS cookbook)

resources:
  Resources:
    OrdersQueue:
      Type: AWS::SQS::Queue
      Properties:
        QueueName: orders
```

### Scope IAM explicitly

The Serverless Framework attaches a broad default role. Tighten it to what each Benzene package needs (see [AWS IAM Permissions](../aws-iam-permissions.md)):

```yaml
provider:
  iam:
    role:
      statements:
        - Effect: Allow
          Action: [sqs:ReceiveMessage, sqs:DeleteMessage, sqs:GetQueueAttributes]
          Resource: arn:aws:sqs:eu-west-1:123456789012:orders
```

### Other event sources

Benzene's Lambda routers cover API Gateway (v1/v2), SQS, SNS, EventBridge, DynamoDB Streams, Kinesis, S3, and Kafka. Each maps to a Serverless Framework event key (`httpApi`, `sqs`, `sns`, `eventBridge`, `stream`, `s3`, …). The pattern is always the same: add the event in `serverless.yml`, add the matching `.UseXxx(...)` in `Configure`.

## Further Reading

- [Getting Started with AWS](../getting-started-aws.md) — the SAM deploy path, the "bare metal" entry point, and IAM per event source
- [Terraform code generator](../terraform.md) — derives Lambda/IAM/EventBridge infra *from* your `[Message]` handlers, which removes the `events:` ↔ `.UseXxx(...)` sync seam entirely (the code→infra alternative to `serverless.yml`'s config→infra model)
- [Project Templates](../getting-started-templates.md) — generating the Benzene AWS Lambda starter this cookbook deploys
- [Handling SQS Message Failures](handling-sqs-failures.md) — partial batch failure reporting for the `sqs` event source
- [Integration Testing Lambda Functions](testing-lambda-functions.md) — testing the handler in-process, independent of how it's deployed
- [Serverless Framework — AWS Lambda events](https://www.serverless.com/framework/docs/providers/aws/events/apigateway) — the full `events:` reference
