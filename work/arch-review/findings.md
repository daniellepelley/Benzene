# Benzene architecture / anti-pattern sweep — consolidated

Four `architecture-reviewer` agents swept the codebase in parallel (core+DI, AWS adapters,
Azure+Kafka+gRPC+Rabbit, cross-cutting+mesh+saga). Findings below are de-duplicated and ranked by
architectural impact. Every reviewer independently confirmed the codebase's *documented* tradeoffs
are deliberate and sound — this list is the genuine smells, not those.

Overall verdict from all four: **APPROVE WITH SUGGESTIONS.** No boundary/public-API breakage. The
recurring theme is **convergence debt** — newer code standardized on a good pattern that older
siblings (and a few seams) haven't caught up to yet — plus a couple of real hot-path costs.

---

## Systemic themes (flagged by multiple reviewers)

### T1. Two generations of adapter plumbing; SQS + DynamoDB are the holdouts — HIGH
Newer batch/record adapters (SNS, S3, Kafka, EventBridge, Kinesis) use: `IHasMessageResult` +
`MessageHandlerResultSetterBase<T>` (a one-liner) + declarative transport tagging via
`new TransportMiddlewarePipeline<T>(TransportNames.X, pipeline)`. **SQS and DynamoDB still use the
old shape**: a bare `bool? IsSuccessful` on the context, a hand-rolled result setter, and a
**magic-string** `setCurrentTransport.SetTransport("sqs")` / `"dynamodb"` inside the fan-out loop.
Those literals directly defeat what `TransportNames` exists to prevent (its doc says it's the single
source of truth so the runtime tag and the DI `ITransportInfo` registration "can't silently drift" —
and SQS's *own* registration already uses `TransportNames.Sqs`, so one package sets the tag two ways).
The `bool?` vs `IHasMessageResult` fork also forces outcomes to be read two ways (`!= true` vs
`== false`), which hides a real policy difference (T-null below).
*Files:* `Benzene.Aws.Lambda.Sqs/SqsApplication.cs`, `SqsMessageContext.cs`,
`Benzene.Aws.Lambda.DynamoDb/DynamoDbApplication.cs`, `DynamoDbRecordContext.cs`.
*Direction:* converge the two holdouts onto the newer shape.

### T2. Duplicated outbound wire-mapping between the two converter families — MED (has already drifted)
Every egress transport ships a pair — `XxxContextConverter<T>` (the `IBenzeneMessageClient` path) and
`OutboundXxxContextConverter` (the `IBenzeneMessageSender`/`OutboundContext` path) — whose
`CreateRequestAsync` bodies are near-identical (header→attribute loop, empty-skip, topic-attribute
guard, response map). The duplication has **already produced real drift**:
- SNS FIFO (`MessageGroupId`/`DeduplicationId`) + numeric-attribute typing exist on
  `SnsContextConverter<T>` only; `OutboundSnsContextConverter` hardcodes `DataType="String"` → the
  same logical send behaves differently by entry point.
- The 10-attribute cap guard exists on both SQS converters but neither SNS converter (SNS has the
  same limit → large header sets fail opaquely on SNS, cleanly on SQS).
This spans SQS/SNS/ServiceBus/EventHub/EventGrid/QueueStorage (≈6 transports × 2 copies).
*Direction:* one internal `MessageAttributeBuilder`/payload builder per transport, fed by a tiny
accessor over the two context shapes (they differ only in how topic/headers/request are exposed).

### T3. Per-send serializer + converter allocation in single-message egress clients — MED (perf)
Kafka and RabbitMQ hold a `static readonly ISerializer` (their CLAUDE.md documents this as a fix:
a fresh `JsonSerializer()` per send defeats System.Text.Json's per-`JsonSerializerOptions` metadata
cache). The **4 Azure** (ServiceBus/EventHub/QueueStorage/EventGrid) and **3 AWS**
(Sqs/Sns/EventBridge) single-message clients still allocate a fresh serializer **and** converter on
every `SendMessageAsync` — and their own **batch** siblings already cache it, so within a package the
single-send path is the odd one out.
*Direction:* hoist the shared static serializer + build the converter once, matching Kafka/RabbitMQ.

### T4. Missing shared abstractions → boilerplate copied across N packages
Four distinct copy-paste families, each a candidate for one shared helper:
- **6 `*LambdaHandler` classes** are near-identical, discriminating on scattered `aws:*` string
  literals with style drift already visible (Kinesis `Records.Count > 0` vs everyone else's
  `.Any()`). → a `RecordBatchLambdaHandler<TEvent,TRecord>` base + centralized `aws:*` constants.
- **Consumer DI-registration bundle** (`TryAddScoped<JsonSerializer>`, `PresetTopicHolder`, the four
  getters, version getter, media negotiation, request mapper, `TransportInfo`) is repeated verbatim
  in every `AddServiceBusConsumer`/`AddEventHubConsumer`/`AddRabbitMq`/`AddAzureServiceBus` — and the
  docs warn a missing entry here "surfaces only as messages never being handled" (silent). → one
  `AddBenzeneConsumerMappers<TContext>(...)` in `Benzene.Core.MessageHandlers`.
- **No shared self-hosted-worker base** — Kafka/ServiceBus/EventHub/Cosmos/RabbitMQ each reinvent the
  same `StartAsync`-starts / `StopAsync`-drains skeleton, the same fields, and the same error-logging
  block. T-log and T-factory below are direct symptoms of this gap. → an abstract `BenzeneWorkerBase`.
- **Function-vs-worker getters** for ServiceBus are byte-for-byte duplicated and already drifting
  (worker's topic getter has `string?`/Missing-topic handling the Function twin lacks). Package
  isolation (Functions SDK) is a legitimate reason not to share the *assembly*, but a linked source
  file or shared internal getter would keep the twins in lockstep.

---

## Notable single-reviewer findings

### C1. `TryGetService`/`TryResolve` implemented as exceptions-as-control-flow — MED-HIGH
Both DI adapters (`MicrosoftServiceResolverAdapter`, `AutofacServiceResolverAdapter`) implement the
*optional* resolution path by calling the *required* API inside try/catch. This is the seam the core
uses for every "is this optional feature wired?" check — run per request in
`MiddlewarePipeline.GetMiddlewareFactory`, per event in `SeedCancellationToken`, and at dozens of
`TryResolve` sites. When a service isn't registered (the normal "feature off" case) every call throws
+ catches a first-chance exception on a hot path. Both containers have non-throwing primitives
(`IServiceProvider.GetService` returns null; Autofac `ResolveOptional`).
*Direction:* implement `TryGetService` over the non-throwing primitive; reserve the throw for `GetService`.

### C2. Azure workers resolve the logger via a per-error DI scope — MED
The 3 Azure workers (+ the Function ServiceBus path) spin up a fresh `IServiceResolver` scope on
**every log call** just to fetch a singleton `ILogger<T>` — worst exactly under a fault storm.
Kafka/RabbitMQ resolve it once in the factory and inject it. *Direction:* inject the logger; delete
the per-error `CreateScope()` blocks. (A symptom of T4's missing worker base.)

### C3. Overlapping "result" abstractions — MED
Three coexist: `IBenzeneResult<T>` (rich), `IMessageHandlerResult<T>` (wraps it), and a self-described
**legacy** `IMessageResult` (bare pass/fail) — yet the live ack path runs on the legacy type via
`IHasMessageResult`. A reader must learn which of three "result" types each seam speaks.
*Direction:* collapse `IMessageResult` into a derived read of `IMessageHandlerResult`, or `[Obsolete]`
it with a migration path.

### C4. ServiceBus settlement capability diverges: worker vs Function trigger — MED
The self-hosted worker supports full explicit settlement (`ServiceBusSettlementHolder`:
Complete/Abandon/**DeadLetter/Defer**); the Function trigger supports only Complete/Abandon — even
though the `ServiceBusMessageActions` it already holds exposes `DeadLetter`/`Defer`. So a handler that
dead-letters a poison message on the worker silently loses that ability behind a trigger; the two
adapters for one transport aren't substitutable. The outcome→settlement rule is also independently
reimplemented in both (drift risk). *Direction:* give the trigger the same holder seam + a shared
decision helper.

### C5. Client-factory seam has two contradictory ownership shapes — LOW-MED
"Factory" means create-per-call for Kafka/RabbitMQ/Cosmos, but `ServiceBusClientFactory`/
`EventProcessorClientFactory` just return a pre-injected singleton. Disposal then diverges: the
ServiceBus worker disposes a `ServiceBusClient` it didn't create (making it **non-restartable** — a
second `StartAsync` gets the disposed singleton), while the EventHub worker disposes nothing.
*Direction:* make the two Azure seams create-per-`Create()` like the others, or standardize "never
dispose what you didn't create" and drop the ServiceBus disposal.

### C6. Invocation-identity wiring is an incomplete rollout — MED
`UseSqs`/`UseSns`/`UseKafka` auto-wire `UseBenzeneInvocation()`; `UseS3`/`UseEventBridge`/`UseDynamoDb`
don't — yet each dispatches inside its own `CreateScope()`, the exact disconnected-scope condition the
wired packages' docs cite as why `IBenzeneInvocation` "came back null." So `IBenzeneInvocation` is
unresolved in those three handlers. *Direction:* wire it (stable id: S3 object key, EventBridge
`event.Id`, DynamoDB sequence number) or document the opt-out.

### C7. Null-outcome settlement is re-decided per adapter with no named policy — MED
Each response-producing adapter hand-codes what an *unset* outcome means (SQS/DynamoDB → failure;
Kafka → success/skip, documented). The divergence is implicit because outcomes are read off two
context shapes (T1); once T1 is unified this becomes a one-line named policy per adapter
(`NullOutcome.TreatAsFailure` vs `TreatAsProcessed`) instead of an incidental `!= true` vs `== false`.

### C8. Library-hygiene: no `ConfigureAwait(false)` in core; ValueTask/Task inconsistency — LOW-MED
Core `await`s (pipeline, router, message handler) don't use `ConfigureAwait(false)` — fine under
SynchronizationContext-free hosts, but a shipped library (esp. one with a self-host worker) shouldn't
rely on the host. Separately, the response-write chain mixes `ValueTask` (`IResponseHandler`) and
`Task` (renderer/setter/middleware), so the one `ValueTask` optimization is immediately re-`await`ed
away.

---

## Low / cleanup (cheap, low-risk)
- `SqsMessageTopicGetter.GetFromAttributes` dereferences `MessageAttributes` unguarded — a latent NRE
  the sibling `SqsMessageHeadersGetter` was *just* hardened against (SNS routes both through the
  null-safe `SnsUtils`). One-line fix.
- Autofac adapter: the `if (typeof(T) == typeof(IServiceResolver))` block is written **twice** (second
  unreachable); `Dispose` assigns `null` to a non-nullable field (post-dispose NRE, nullability lie).
- Microsoft vs Autofac adapters drift: asymmetric special-casing of `IServiceResolverFactory` in
  `GetService` vs `TryGetService`; Microsoft news a fresh factory per call, Autofac returns a stored one.
- `HttpRequest.{Method,Path,Headers}` non-nullable with no initializer (public API lies about null).
- `MiddlewarePipelineBuilder` CLAUDE.md claims "immutable, returns new instance" — it mutates a shared
  list and returns `this` (doc drift; also noted in the earlier bug-hunt log).
- `Debug.WriteLine` leftovers on hot paths (`MessageRouter.cs:99`, `MicrosoftServiceResolverAdapter`).
- Dead reflection-era code in `MessageClientSdkBuilder` (`_propertyTypeMapping` + `GetTypeName(Type)`).
- `ValidateOutboundRouting` matches any type with a `public static string[] RequiredTopics` field
  across all loaded assemblies — no marker interface/attribute (fragile, causes documented test
  pollution). Gate on a real contract.
- S3 package uses `Benzene.Aws.S3` namespace/assembly while every sibling is `Benzene.Aws.Lambda.*`.
- `KafkaMessageHeadersGetter` fabricates a synthetic `"topic"` header (bare literal) no sibling getter
  does; the `"topic"` default-attribute const is redeclared in several places.
- `readonly`-able fields never reassigned (`MessageHandler._defaultStatuses`, etc.).

---

## Verified clean / deliberate (reassurance — not smells)
Saga engine (clean state machine; orchestration/step/context/store cleanly separated); Resilience
(composes as middleware; retry-only split documented); Diagnostics (tracing/timing/correlation all
composable middleware, not baked into adapters); serialization + media negotiation (proper strategy
pattern); health checks (consistent decorator stack); Mesh (clean parallel discovery/aggregation/
artifact-store seams); and — emphasized by two reviewers — the **middleware/context/scoped-holder
discipline is strong and consistently applied** (`PresetTopicHolder`/`ServiceBusSettlementHolder`/
`HttpRequestBodyBuffer` scoped holders instead of context markers; `IHasMessageResult` is the one
documented, deliberate purity carve-out). The many documented tradeoffs (AckMode defaults, fan-in vs
fan-out, SDK-free packages, no Cosmos egress, non-generic `OutboundContext`) are sound.

---

## 2026-07-21 — Fresh security/correctness hunt (implemented + deferred)

Implemented and pushed to main this pass:
- Native AMQP batch leak (ServiceBus/EventHub batch clients) — try/finally around the
  ServiceBusMessageBatch/EventDataBatch lifetime.
- XML entity-expansion DoS (Benzene.Xml) — XmlReader.Create with DtdProcessing.Prohibit +
  null XmlResolver on the untrusted deserialize path.
- Path traversal in FileSystemMeshArtifactStore — resolve-and-contain check on both
  PublishAsync/TryReadAsync (report.Name flows in from an untrusted push body).
- SSRF/URL-restructuring in the Azure/Kubernetes discovery providers — sanitize host (bare
  authority), scheme (http/https only), and path overrides (must start with '/'); fall back to
  the safe default rather than aborting the sweep.
- Codegen: CSharpTypeName NRE on plain object + int64→int truncation; MessageHandlerSourceGenerator
  made genuinely incremental (dropped unused CompilationProvider.Combine, IEquatable model) and
  emitted topic/version literals escaped via SymbolDisplay.FormatLiteral.
- CORS: refuse Access-Control-Allow-Credentials when AllowedDomains contains "*" (origin-reflection
  hole; matches ASP.NET Core). Corrected the CorsSettings/Http docs that called it "safe".

Deferred for maintainer decision (policy / larger surface, not silently changed):
- **SchemaCompatibilityComparer gaps** — the backward-compat gate only diffs
  type/format/properties/required. It does NOT flag enum-value changes (removing an allowed value
  is breaking), nullable flips (required non-null → nullable on a response, or nullable → required
  on a request), or facet tightening (maxLength shrink, minLength/minimum raise, pattern add). These
  are false-negatives: a breaking contract change can pass the gate. Extending it means new public
  SchemaChangeKind values + default rule classifications per direction (request vs response), which
  is a policy call (what counts as breaking for whom). Left to the maintainer; the comparison
  scaffold (CompareSchemas recursion, SchemaChangeKind, SchemaCompatibilityRules) is where it'd slot in.
- **CRLF response-header injection (defence-in-depth, LOW)** — the API Gateway/self-host response
  adapters write header values through without stripping CR/LF. Header values are Benzene- or
  handler-sourced today (not raw request echoes), so it's not a confirmed live vector; worth a
  central strip if a handler ever reflects request data into a response header.
