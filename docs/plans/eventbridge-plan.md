# Benzene EventBridge Integration Plan

## Context

Amazon EventBridge is the one first-class AWS eventing service Benzene has no support for — the
AWS roadmap tracks it explicitly as unbuilt ("Real EventBridge/CloudWatch Events support does not
exist anywhere in Benzene"), made worse historically by a package *named* EventBridge that actually
handled S3 events (since renamed to `Benzene.Aws.Lambda.S3`).

It is also the best conceptual fit of any AWS service: Benzene's model is topic → handler routing,
and EventBridge events natively carry a routing key (`detail-type`) plus an origin (`source`). No
`topic` message attribute needs bolting on, unlike SQS/SNS.

Scope: **inbound** (EventBridge → Lambda → Benzene handlers) and **outbound** (a Benzene message
client publishing via `PutEvents`), plus test helpers, tests, docs, and spec updates. Terraform
rule generation from `[Message]` topics is explicitly out of scope (future work).
*(Since done — see `docs/plans/terraform-eventbridge-rules-plan.md`.)*

## Verified facts this plan relies on

- EventBridge invokes a Lambda target with a **single event per invocation** (not a `Records`
  batch): `{version, id, "detail-type", source, account, time, region, resources, detail}`. So the
  inbound binding uses the single-context `MiddlewareApplication<TEvent, TContext>`
  (`src/Benzene.Core.Middleware/MiddlewareApplication.cs`), not `MiddlewareMultiApplication` —
  one pipeline invocation, one DI scope per event. The invocation is asynchronous — no response
  mapping (same as `SnsLambdaHandler`).
- The inbound blueprint is `src/Benzene.Aws.Lambda.Sns/` end to end: `AwsLambdaMiddlewareRouter<TRequest>`
  (deserialize-probe + `CanHandle` + fall-through to `next()`), `UseXxx(action)` extension,
  `AddXxx()` DI extension with the four context-keyed getters/setters +
  `MultiSerializerOptionsRequestMapper` + `TransportInfo`, `XxxRegistrations : RegistrationsBase`,
  and a `*.TestHelpers` sibling with `MessageBuilderExtensions.AsXxx<T>()`.
- The outbound blueprint is `src/Benzene.Clients.Aws/Sns/`: send context wrapping the SDK
  request/response, client middleware, `IContextConverter`, `IBenzeneMessageClient`, and
  `UseXxxClient`/`UseXxx<T>` extensions.
- `AwsLambdaMiddlewareRouter.TryExtractRequest` deserializes with `DefaultLambdaJsonSerializer`
  (System.Text.Json-based), so `[JsonPropertyName("detail-type")]` and `JsonElement` properties
  work on the probe type.
- Routing probes are chained: each `UseXxx` middleware deserializes the raw payload and passes to
  `next()` when `CanHandle` is false. SQS/SNS/S3 payloads have `Records` and no `detail-type`;
  API Gateway has no `detail-type`; `BenzeneMessage` has `topic`. `detail-type` + `source`
  presence is a sufficient discriminator.
- `PutEventsResponse` exposes `HttpStatusCode` and `FailedEntryCount` (+ per-entry `ErrorCode`/
  `ErrorMessage`); a request can succeed at HTTP level while individual entries fail, so success
  requires both `HttpStatusCode == OK` and no failed entries. (Comparisons are written with
  relational operators only, so the code compiles whether the SDK models the count as `int` or
  `int?`.)

## ⚠️ FLAGS — approved by approving this plan

- **New NuGet dependency:** `AWSSDK.EventBridge` added to the existing `Benzene.Clients.Aws`
  project (outbound only). The **inbound** package deliberately adds **no** new dependency: the
  event envelope is a stable, documented shape, so it's modeled as Benzene's own POCO
  (`EventBridgeEvent`) rather than referencing `Amazon.Lambda.CloudWatchEvents`.
- **Solution structure:** two new projects added to `Benzene.sln`:
  `src/Benzene.Aws.Lambda.EventBridge`, `src/Benzene.Aws.Lambda.EventBridge.TestHelpers`.

## Design decisions (final)

- **E1 Topic mapping:** topic = `detail-type`, verbatim. `source` is metadata, not part of the
  topic. Handlers declare `[Message("order.created")]` matching the publisher's `DetailType`.
- **E2 Event probe:** `EventBridgeEvent` POCO with `[JsonPropertyName]` attributes and
  `JsonElement Detail`; `CanHandle` = `DetailType` and `Source` both non-null.
- **E3 Body:** the raw JSON text of `detail` — the domain payload, handed to the normal
  JSON request mapper. `detail` must be a JSON object for routing to be useful; other shapes pass
  through as raw text and fail at request-mapping like any malformed body would.
- **E4 Headers (the wire contract for this binding):**
  - *Envelope metadata*, prefixed to avoid collisions: `eventbridge-id`, `eventbridge-source`,
    `eventbridge-account`, `eventbridge-region`, `eventbridge-time`, `eventbridge-detail-type`.
  - *Benzene wire headers* (correlation, `traceparent`, tenant headers, ...): EventBridge has no
    native per-message attributes (unlike SQS/SNS `MessageAttributes`), so the outbound client
    embeds the request's headers into `detail` under a reserved top-level key
    **`_benzeneHeaders`** (string→string object), only when there are headers to send and the
    payload serializes to a JSON object. The inbound headers getter lifts `_benzeneHeaders` back
    out (unprefixed, winning over prefixed envelope keys). Non-Benzene consumers see one extra,
    ignorable JSON field; consumers filtering on `detail` fields are unaffected.
- **E5 Inbound shape:** one pipeline invocation + one DI scope per event; fire-and-forget (no
  response stream mapping). Transport name: `"eventbridge"`.
- **E6 Outbound client:** `EventBridgeBenzeneMessageClient : IBenzeneMessageClient` configured
  with a fixed `source` and optional `eventBusName` (default bus when null); topic →
  `PutEventsRequestEntry.DetailType`, serialized message (+ embedded headers per E4) → `Detail`.
  Result: `Accepted` when HTTP OK and no failed entries; a failed entry maps to
  `ServiceUnavailable` carrying the entry's `ErrorCode`/`ErrorMessage`; exceptions map to
  `ServiceUnavailable` (standard client behavior).
- **E7 Registration parity:** `AddEventBridge()`, `UseEventBridge(action)`,
  `EventBridgeRegistrations`, mirroring SNS exactly. TestHelpers:
  `MessageBuilder.Create(topic, msg).AsEventBridge()` produces a realistic event with the topic as
  `detail-type` and the builder's headers embedded per E4.

## Phases

### Phase 1 — Inbound: `src/Benzene.Aws.Lambda.EventBridge/`

`EventBridgeEvent.cs`, `EventBridgeContext.cs` (`: IHasMessageResult`),
`EventBridgeApplication.cs` (`MiddlewareApplication<EventBridgeEvent, EventBridgeContext>`,
transport `"eventbridge"`), `EventBridgeLambdaHandler.cs`
(`AwsLambdaMiddlewareRouter<EventBridgeEvent>`, `CanHandle` per E2, fire-and-forget),
`EventBridgeMessageTopicGetter/BodyGetter/HeadersGetter.cs` (per E1/E3/E4),
`EventBridgeMessageMessageHandlerResultSetter.cs`, `DependencyInjectionExtensions.cs`
(`AddEventBridge()`), `Extensions.cs` (`UseEventBridge(...)`), `EventBridgeRegistrations.cs`,
`CLAUDE.md`, csproj (ProjectReference: `Benzene.Aws.Lambda.Core` only).

### Phase 2 — Outbound: `src/Benzene.Clients.Aws/EventBridge/`

`EventBridgeSendMessageContext.cs`, `EventBridgeClientMiddleware.cs`,
`EventBridgeContextConverter.cs` (E4 embedding via `System.Text.Json.Nodes`),
`EventBridgeBenzeneMessageClient.cs`, `Extensions.cs` (`UseEventBridgeClient`,
`UseEventBridge<T>`). Add `AWSSDK.EventBridge` to `Benzene.Clients.Aws.csproj`.

### Phase 3 — TestHelpers, tests, docs, spec

- `src/Benzene.Aws.Lambda.EventBridge.TestHelpers/` with `AsEventBridge<T>()`.
- Tests in `test/Benzene.Core.Test/Aws/EventBridge/` (+ csproj references):
  pipeline test (mirrors `SnsMessagePipelineTest.Send`), getter tests (topic from detail-type,
  body from detail, `_benzeneHeaders` lift + envelope prefixes), router test (EventBridge payload
  handled / SQS-shaped payload falls through), outbound converter tests (topic→DetailType, header
  embedding incl. the no-headers and non-object-payload cases), client test with mocked
  `IAmazonEventBridge` (Accepted on success; failed entry → ServiceUnavailable with error detail).
- `Benzene.sln`: register both new projects.
- Docs: `docs/clients.md` EventBridge section; `docs/specification/transport-bindings.md` catalog
  entry; `docs/specification/wire-contracts.md` §2 note for `_benzeneHeaders`; tick the roadmap.

## Acceptance

- An EventBridge-invoked Lambda routes `detail-type` to a `[Message]` handler through the shared
  middleware pipeline, with `detail` as the request body and headers per E4.
- A Benzene service publishes domain events via `IBenzeneMessageClient` to EventBridge with
  correlation/trace headers that a receiving Benzene service recovers.
- Non-EventBridge payloads fall through `UseEventBridge` untouched; all existing event sources
  are unaffected.
- `dotnet build Benzene.sln` and the full test suite green (CI-verified; no local SDK here).
