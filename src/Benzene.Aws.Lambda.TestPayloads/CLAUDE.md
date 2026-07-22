# Benzene.Aws.Lambda.TestPayloads

## What this package does
Opt-in **AWS transport dressing** for Benzene's runtime `test-payloads` endpoint. The runtime-safe
core (`Benzene.Schema.OpenApi`'s `UseTestPayloads()` / `TestPayloadsBuilder`) serves each domain
topic's deterministic example payload wrapped only in the portable `benzene-message` envelope — it
carries **no AWS coupling**. This package adds the SNS/SQS/API-Gateway dressings so a deployed Lambda
self-serves example payloads dressed for the transports it's actually wired to, ready to paste into
the Lambda test console — the runtime counterpart of the build-time `Benzene.CodeGen.LambdaTestTool`.

This is the split the maintainer's `work/runtime-test-payloads-plan.md` decision **1(c)** called for:
runtime-clean core, AWS dressing in a separate opt-in package, so a non-AWS or minimal service never
pulls the `Amazon.Lambda.*Events` contracts into its runtime graph.

## Key types
- `ITestPayloadDresser` / `TestPayloadDressingContext` — **live in the core** (`Benzene.Schema.OpenApi`),
  not here. The seam is `TestPayloadTopic.Payloads` (a `transport → object` dictionary): the builder
  resolves every registered `ITestPayloadDresser` from DI and folds each one's output in under its
  `Transport` key, alongside the always-present `benzene-message` entry.
- `SnsTestPayloadDresser` (`sns`), `SqsTestPayloadDresser` (`sqs`), `ApiGatewayTestPayloadDresser`
  (`api-gateway`) — each mirrors the corresponding `MessageBuilder.AsSns()`/`AsSqs()` /
  `HttpBuilder.AsApiGatewayRequest()` shape, reusing the context's already-serialized example body so
  every transport agrees on the payload. Each **decides its own applicability** and returns `null` to
  skip: SNS/SQS skip a host not wired for that transport (`context.SupportsTransport(...)`), API Gateway
  skips a topic with no HTTP mappings (and dresses the first mapping as a representative request).
- `AwsEventJson` (internal) — serializes the AWS event POCO to a `JToken` so the manifest's camelCase
  serializer embeds it **verbatim**, preserving the canonical AWS event property casing the Lambda
  console expects (a raw POCO would be re-cased to camelCase).
- `Extensions`:
  - `AddAwsTestPayloadDressers(this IBenzeneServiceContainer)` — registers the three dressers.
  - `UseAwsTestPayloads<TContext>(this IMiddlewarePipelineBuilder<TContext>)` — one-call opt-in:
    registers the core `test-payloads` handler **and** the AWS dressers. Nothing is exposed unless
    called, and it reveals no more than the already-public `spec` topic — no `AllowInProduction` gate is
    needed here (that gate belongs to any *dispatch* feature that fires real handlers, not to this
    read-only payload catalogue).

## Determinism
The endpoint is polled/cacheable like `spec`, so output must be stable per build. The dressers use no
randomness — notably `SqsTestPayloadDresser` uses a fixed placeholder `MessageId`
(`00000000-0000-0000-0000-000000000000`) rather than the live `AsSqs` helper's random `Guid`.

## Dependencies
- `Benzene.Schema.OpenApi` (project reference) — for the `ITestPayloadDresser` seam and the core
  `UseTestPayloads()` extension that `UseAwsTestPayloads` composes.
- `Amazon.Lambda.SNSEvents` / `Amazon.Lambda.SQSEvents` / `Amazon.Lambda.APIGatewayEvents` — the
  lightweight event-contract POCOs (no AWS SDK, no `Amazon.Lambda.TestUtilities`, no Roslyn). These are
  exactly what stays out of the runtime-safe core.

## Tests
`test/Benzene.Aws.Tests/TestPayloads/TestPayloadDressersTest.cs` — round-trips each dresser's `JToken`
back through the AWS POCO and asserts the envelope shape + the skip (null) behaviour. The core seam
(builder folds dressers, honours null-skip, exposes supported transports) is covered by
`test/Benzene.Core.Test/Autogen/Schema/OpenApi/TestPayloads/TestPayloadsBuilderTest.cs`.

## When to use
Add `UseAwsTestPayloads()` (in place of the core `UseTestPayloads()`) on an AWS Lambda host that
receives over SNS/SQS/API Gateway, when you want the deployed function to self-serve transport-dressed
example payloads for manual testing. Leave it off (or use the core `UseTestPayloads()`) for the
portable `benzene-message` envelope only.
