# Design — High-severity cloud/transport fixes (#25–#28)

Concrete designs for the four High design-first issues, grounded in the current code (exact
types/seams verified). Each: **approach**, **API/type changes**, **backward-compat**, **tests**,
**effort/risk**, **sequencing**.

Shared principles: additive/opt-in; new event shapes slot in as *parallel routers/handlers* rather
than rewrites; string→byte body work widens the existing seams (some already half-present) rather
than replacing them.

---

## #25 — API Gateway: HTTP API v2 payload + binary request/response bodies

### Current state (verified)
- Only `APIGatewayProxyRequest`/`APIGatewayProxyResponse` (v1.0, `Amazon.Lambda.APIGatewayEvents`).
- `ApiGatewayLambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayProxyRequest>`; `CanHandle` = `request?.HttpMethod != null` (a v1-only field → v2 events decline and fall through, then the entrypoint throws "event type not recognized").
- The outer `AwsEventStreamContext` pipeline is a **list of routers**; each rewinds the shared stream, best-effort-deserializes into its own `TRequest`, and applies a shape predicate. First match wins and is terminal. **Adding a second, mutually-exclusive router is fully supported** — no entrypoint change needed.
- Body is `string` end-to-end. `ApiGatewayMessageBodyGetter.GetBody` UTF-8-decodes a base64 body (lossy for true binary — the only read of request `IsBase64Encoded`). `ApiGatewayResponseAdapter.SetBody` is `string`; response `IsBase64Encoded` is **never written**.
- `IBenzeneResponseAdapter<T>` already declares a **default `SetBody(T, ReadOnlyMemory<byte>)`** that UTF-8-decodes to the string overload — the write-side binary seam is half-present.

### Approach — two orthogonal, independently-shippable pieces

**(A) v2 payload support = a parallel router (no entrypoint change).** New package-internal set
mirroring the v1 types, keyed off a v2-only discriminant:
- `ApiGatewayV2Context : IHttpContext` wrapping `APIGatewayHttpApiV2ProxyRequest`/`...V2ProxyResponse`.
- `ApiGatewayV2LambdaHandler : AwsLambdaMiddlewareRouter<APIGatewayHttpApiV2ProxyRequest>` with
  `CanHandle(r) => r?.Version == "2.0" || r?.RequestContext?.Http?.Method != null` (mutually exclusive
  with v1's `HttpMethod != null`, so registration order is irrelevant).
- v2 request/response/topic/headers/version getters + enricher. Key differences to handle in the
  adapters: method/path come from `RequestContext.Http.{Method,Path}`; `RawQueryString`;
  `Cookies[]` (array, not a header) → map into request headers on read and to response
  `Cookies[]`/`multiValueHeaders` on write; v2 single-value comma-joined `Headers`.
- `Extensions.UseApiGatewayV2(action)` — identical wiring to `UseApiGateway` (register, build inner
  `ApiGatewayV2Context` pipeline, `app.Use(new ApiGatewayV2LambdaHandler(...))`).
- Route-finding, health-check helpers, media negotiation are all `IHttpContext`-generic and reused.

An app can call **both** `UseApiGateway` and `UseApiGatewayV2` in one Lambda (REST + HTTP API), or
just the one matching its front door. Simple "is it up" apps that use HTTP API defaults now work
instead of hard-failing.

**(B) Binary bodies = widen the body seams.** This is the harder, cross-cutting part; ship after (A):
- **Request:** add a byte-oriented read alongside the existing string path. The body getter already
  knows `IsBase64Encoded`; when the negotiated/declared content type is non-text, expose the *raw
  bytes* (`Convert.FromBase64String(Body)` without the UTF-8 round-trip) via a new
  `IMessageBodyBytesGetter<T>` (a sibling to `IMessageBodyGetter`) that handlers/mappers can consume.
  Keep `GetBody` (string) for text — only decode UTF-8 for textual content types.
- **Response:** override `SetBody(context, ReadOnlyMemory<byte>)` in the response adapter to store raw
  bytes + set `APIGatewayProxyResponse.IsBase64Encoded = true` and base64-encode the body at
  finalize. The default interface overload makes this a localized change. Add an
  `IsBase64Encoded`-aware finalize.
- This same widening benefits the self-host HTTP server (#28) — do it once in the shared
  `Benzene.Http`/response-adapter contracts.

### Backward-compat
Fully additive. v1 path untouched (v2 router only claims v2 events). Binary read is a new getter
(text path unchanged); binary response uses the already-present default overload.

### Tests
- v2: a v2 proxy event routes through `UseApiGatewayV2` to a handler (method/path/query/cookies mapped
  correctly); a v1 event still routes through `UseApiGateway` when both are registered; a v2 event
  with `UseApiGateway` only → declines (documented) rather than mis-handling.
- Binary: a base64 image request body reaches the handler byte-identical; a handler returning bytes
  produces `IsBase64Encoded=true` + base64 body; a text response stays `IsBase64Encoded=false`.

### Effort / risk
(A) Medium — mechanical mirror of the v1 adapter set; low risk (isolated new router). (B) Medium-High
— touches shared body contracts; do it once for HTTP broadly. **Sequence: ship (A) first** (closes the
hard-fail), then (B) as a cross-cutting "binary HTTP bodies" change spanning API Gateway v1/v2 + ALB +
self-host. Also add ALB target-group support cheaply on the v1 shape as a follow-up (near-identical +
`statusDescription`/`multiValueHeaders`).

---

## #26 — Azure Functions: same-trigger dispatch collision

### Current state (verified)
- `AzureFunctionApp.HandleAsync<TRequest>` linearly scans `_apps` (`IEntryPointMiddlewareApplication[]`)
  and returns the **first** element matching `is IEntryPointMiddlewareApplication<TRequest>`. The
  two-generic overload matches the concrete `EntryPointMiddlewareApplication<TRequest,TResponse>`
  (an existing asymmetry). `IEntryPointMiddlewareApplication<in TEvent>` is **contravariant**, widening
  the match set.
- Registration (`IAzureFunctionAppBuilder.Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>)`)
  and `_apps` carry **no name/key** — dispatch is purely type-based.
- Trigger helpers (`HandleQueueMessages`, `HandleEventGridEvents`, `HandleHttpRequest`, …) forward the
  payload to `HandleAsync` with no identity. The `[QueueTrigger("orders")]` function method is the only
  place the caller knows *which* queue function it is.

### Approach — thread an optional discriminator key end-to-end (additive, type-only fallback)
The key is created in the `Use*` wiring and consumed in `AzureFunctionApp.HandleAsync`; it never needs
to leave `Benzene.Azure.Function.Core` conceptually. Three additive seams:

1. **Registration carries a key.** Add `IAzureFunctionAppBuilder.Add(string key, Func<...> factory)`
   (keep the keyless overload → `key = null`). Store `(string? Key, Func<...>)` pairs; `AzureFunctionApp`
   keeps `(string? Key, IEntryPointMiddlewareApplication)[]`. Each `UseQueueStorage`/`UseEventGrid`/…
   gains an optional `string name = null` that flows to `Add(name, …)`. The name is naturally the
   queue/topic/function name the author already knows.

2. **Dispatch helpers pass the key.** Add an optional `string name = null` to `HandleQueueMessages`,
   `HandleEventGridEvents`, `HandleServiceBusMessages`, `HandleKafkaEvents`, `HandleEventHub`,
   `HandleBlob`, `HandleTimer` (and `HandleHttpRequest`), forwarding to `HandleAsync(payload, name)`.
   The `[QueueTrigger("orders")]` method calls `HandleQueueMessages("orders", messages)`.

3. **Selection matches key when present.** `HandleAsync<TRequest>(request, string name = null)`:
   - If `name != null`: match `pair.Key == name && pair.App is IEntryPointMiddlewareApplication<TRequest>`.
   - If `name == null`: current type-only first-match (unchanged single-function behavior).
   - No match with a name → a clear exception listing registered `(key, shape)` pairs.

Also fix the **overload asymmetry** while here: make the two-generic overload match the interface
`IEntryPointMiddlewareApplication<TRequest>` (or a new `<TRequest,TResponse>` interface) for
consistency, so HTTP (which uses the two-generic path) can also be keyed if ever needed.

### Backward-compat
Fully additive: keyless registration + keyless dispatch = today's exact behavior. Multi-function apps
opt in by naming each `Use*`/`Handle*`. A single function per trigger needs no change.

### Tests
- Two `UseQueueStorage("a", …)` / `UseQueueStorage("b", …)` → `HandleQueueMessages("b", …)` hits the
  **b** pipeline (previously unreachable). Keyless dispatch still hits the first. Unknown name → throws
  with a helpful message. Existing single-function tests unchanged.

### Effort / risk
Medium. Surface touched: `IAzureFunctionAppBuilder.Add`, `IAzureFunctionApp.HandleAsync` (+ impl), and
the per-trigger `Use*`/`Handle*` helpers — all additive. Risk low; the contravariance/asymmetry fix
needs care to not change single-registration resolution.

---

## #28 — Self-host HTTP: binary/streaming bodies, request size limit, startup errors

### Current state (verified)
- `BenzeneHttpWorker.StartAsync`: `new HttpListener()` + `Prefixes.Add(Url)` + **`Start()` run inside a
  detached `Task.Run` above the try/catch**; `StartAsync` returns `Task.CompletedTask` immediately, so
  a bind failure faults an unobserved `_runTask` and only surfaces at `StopAsync` (`await _runTask`) —
  or never.
- `BenzeneHttpConfig`: `Url`, `ConcurrentRequests`, `DrainTimeout` — **no size limit**.
- `HttpListenerMessageBodyGetter.ReadBodyAsync`: `new StreamReader(InputStream).ReadToEndAsync()` —
  UTF-8, **unbounded** (DoS). Request-body buffer chain (`HttpRequestBodyBuffer`,
  `IHttpRequestBodyReader`, `BufferRequestBodyMiddleware`) is `string` end-to-end.
- `HttpContextResponseAdapter.FinalizeAsync`: `Encoding.UTF8.GetBytes(_body)` + `ContentLength64` +
  write. `IBenzeneResponseAdapter.SetBody(ReadOnlyMemory<byte>)` default overload already exists.

### Approach — three independent fixes (ship bind-error + size-limit first; binary with #25(B))

**(a) Surface bind errors (small, high-value).** Restructure `StartAsync`: create the listener,
`Prefixes.Add`, and `Start()` **synchronously in the `StartAsync` body** (before spawning `_runTask`),
so a `Start()` throw propagates out of `StartAsync` and fails host startup loudly. Only the accept loop
+ `DrainAsync` + `Stop/Close` stay in `_runTask` (referencing the already-started `_httpListener`
field). Make `StartAsync` `async` and return after a successful `Start()`.

**(b) Request size limit (small).** Add `long? MaxRequestBodyBytes` to `BenzeneHttpConfig`. Inject the
limit into `HttpListenerMessageBodyGetter` (register it in `AddHttp`; the config is available there).
In `ReadBodyAsync`: reject up front if `Request.ContentLength64 > limit` (→ a 413-mapped failure), and
read via a bounded loop rather than `ReadToEndAsync` so a lying/chunked `ContentLength` can't exceed
the cap. Default `null` = unbounded (unchanged), but document a recommended value.

**(c) Binary bodies (with #25(B), shared).** Widen the body seams once:
- Request: add a byte-returning read to the buffer chain (`HttpRequestBodyBuffer` gains a bytes value;
  `IHttpRequestBodyReader` gains/gets a bytes sibling; `BufferRequestBodyMiddleware` stores bytes).
  `HttpListenerMessageBodyGetter.ReadBodyAsync` reads `Request.InputStream` bytes; decode to string
  only for textual `Request.ContentType`.
- Response: override `SetBody(context, ReadOnlyMemory<byte>)` in `HttpContextResponseAdapter` (store a
  `ReadOnlyMemory<byte>? _bodyBytes`); `FinalizeAsync` writes those bytes directly when present (skip
  the UTF-8 encode). Minimal — the string path stays.
- Streaming responses (chunked) are a later, separate enhancement (`SendChunked`), not required for
  correctness.

**Production-readiness caveat.** Keep the honest CLAUDE.md note. A Kestrel-backed self-host as the
production path is a larger, separate proposal (own issue) — HttpListener stays the dev/test host.

### Backward-compat
(a) changes `StartAsync` to fail fast on a bad bind — strictly better, but a host that previously
"started" with a broken bind now errors at startup (correct). (b) default unbounded = unchanged.
(c) additive (byte paths alongside string).

### Tests
- Bind failure: two workers on the same prefix → the second's `StartAsync` throws (not silent).
- Size limit: a body over the cap → 413/failure, not OOM (drive a real `HttpListener` on a loopback
  port, as the existing `BenzeneHttpWorkerTest` does).
- Binary: a binary upload round-trips byte-identical; a byte response is written verbatim with the
  right `Content-Length`.

### Effort / risk
(a) Small, low risk. (b) Small. (c) Medium (shared with #25). Sequence: (a)+(b) first (they're
self-contained and close the DoS + silent-startup-failure), then (c) as part of the shared binary-HTTP
change.

---

## #27 — Kafka Lambda event source: per-partition ordering + partial-batch response

### Current state (verified)
- `KafkaApplication : MiddlewareMultiApplication<KafkaEvent, KafkaContext>` (the **no-result** base).
  Its mapper does `@event.Records.Values.SelectMany(...)` — flattening the per-topic-partition
  dictionary into one flat `KafkaContext[]` and **discarding partition grouping** — then the base runs
  them all through `BoundedFanOut.WhenAllAsync` (concurrent, no ordering).
- `KafkaLambdaHandler : AwsLambdaMiddlewareRouter<KafkaEvent>` holds `IMiddlewareApplication<KafkaEvent>`
  (no result) and `HandleFunction` **never calls `MapResponse`** — fire-and-forget, no batch response.
- Crucially: `KafkaEvent.Records` is `IDictionary<string, IList<KafkaEventRecord>>` **keyed by
  `"topic-partition"`** — the grouping we need is already in the event. Each record exposes
  `Topic`/`Partition`/`Offset` (Benzene already builds `InvocationId = "{Topic}-{Partition}-{Offset}"`).
- Template: Kinesis uses `StreamMiddlewareApplication<TEvent,TItem,TResult>` (fan-in, ordered single
  run) + `KinesisStreamCheckpointer` (first-uncheckpointed sequence number) + a hand-rolled
  `KinesisBatchResponse { batchItemFailures[].itemIdentifier }`, and `KinesisLambdaHandler` calls
  `MapResponse(context, response)`. The DynamoDB adapter does sequential stop-at-first-failure per shard.

### Approach — process per-partition-sequentially, fan out across partitions, report first failed offset per partition
Combine the two existing patterns: **within** a topic-partition, process records **sequentially in
offset order, stop at first failure** (DynamoDB-style, preserving order); **across** partitions, fan
out concurrently (bounded). Report each failed partition's first-failed record in a `batchItemFailures`
response (Kinesis-style).

**New types (all package-local, no AWS dependency):**
- `KafkaBatchResponse` — mirror `KinesisBatchResponse`:
  ```csharp
  public class KafkaBatchResponse {
      [JsonPropertyName("batchItemFailures")] public List<BatchItemFailure> BatchItemFailures { get; } = new();
      public class BatchItemFailure { [JsonPropertyName("itemIdentifier")] public string ItemIdentifier { get; set; } }
  }
  ```
  ⚠️ **Verify the exact `itemIdentifier` string format AWS expects for the Kafka event source**
  (topic/partition/offset encoding) against the Lambda docs before shipping — this is the one
  wire-contract detail the offline review couldn't confirm. The processing design below is independent
  of that format.
- `KafkaOptions` (mirror `SqsOptions`): `BatchFailureMode { PartialBatchFailure (default), FailWholeBatch }`
  + `MaxDegreeOfParallelism` (now **across partitions**, not across all records) + optional
  `CatchExceptions`.
- `KafkaApplication : IMiddlewareApplication<KafkaEvent, KafkaBatchResponse>` (result-producing),
  replacing the `MiddlewareMultiApplication` base:
  ```
  HandleAsync(event, factory):
    perPartition = await BoundedFanOut.WhenAllAsync(event.Records, options.MaxDegreeOfParallelism, async kvp => {
        foreach (record in kvp.Value.OrderBy(r => r.Offset)) {           // in-partition order
            using scope; run pipeline for new KafkaContext(event, record);
            if (threw) or (context.MessageResult?.IsSuccessful == false)
                return firstFailure(record);                             // stop-at-first-failure
        }
        return null;                                                     // whole partition ok
    });
    var failures = perPartition.Where(x => x != null);
    if (options.BatchFailureMode == FailWholeBatch && failures.Any())
        throw new KafkaBatchProcessingException(...);                    // whole invocation retries
    return new KafkaBatchResponse(failures.Select(ItemIdentifierFor));
  ```
- `KafkaLambdaHandler` now holds `IMiddlewareApplication<KafkaEvent, KafkaBatchResponse>` and calls
  `MapResponse(context, response)` in `HandleFunction` (one-line change; the routing base + entrypoint
  need no change — a returned batch response surfaces exactly like Kinesis's).
- `UseKafka(action, configure)` builds the new application with options (mirrors the S3/EventBridge
  `configure` pattern shipped earlier).

**Trigger requirement.** Like Kinesis/DynamoDB, the partial-batch response is only honored if the event
source mapping has `FunctionResponseTypes=ReportBatchItemFailures`. Document this loudly (and consider
generating it via the Terraform codegen alongside the handler). Without it, only a thrown exception
(FailWholeBatch, or an uncaught fault) retries — same as today.

### Backward-compat
- Returning a `KafkaBatchResponse` is **safe even if the trigger isn't configured** for it — Lambda
  ignores the return value in that case (behaviour unchanged: only a throw retries).
- The **ordering change is behavioral**: records within a partition now run sequentially in offset
  order instead of concurrently. This is the *correct* Kafka semantics and matches AWS's own
  "sequential per partition" guarantee, but throughput-sensitive users who relied on full concurrency
  should tune `MaxDegreeOfParallelism` (across partitions) — document it. This is the intended fix, not
  a regression.

### Tests
- Partition ordering: records of one partition process in offset order (a recorder asserts monotonic
  offsets); two partitions can interleave (concurrency observed across partitions but not within).
- Partial batch: a failure at offset K in partition P reports P's offset-K record in `batchItemFailures`
  and does **not** process P's later records; other partitions complete and aren't reported.
- FailWholeBatch mode throws listing failures. Mirror `SqsBatchFailureModeTest`'s structure (mocked
  pipeline that throws/sets `MessageResult` by record).

### Effort / risk
Medium. Isolated to the Kafka Lambda package (+ its handler's one-line response wiring). Main risk is
the `itemIdentifier` wire format — gate shipping on confirming it against AWS docs (a live test against
a real MSK trigger would validate). The per-partition/stop-at-first-failure logic is a direct
composition of the DynamoDB and Kinesis patterns already in the repo.

---

## Cross-cutting note
Two of these share a **binary-HTTP-body** work item (#25(B) + #28(c)): widen `HttpRequestBodyBuffer` /
`IHttpRequestBodyReader` / `BufferRequestBodyMiddleware` and the response adapter's already-present
`SetBody(ReadOnlyMemory<byte>)` overload once, in `Benzene.Http`, and consume it from both API Gateway
(v1+v2) and the self-host server. Do it as one change rather than twice.

## Suggested sequencing (High tier)
1. **Self-host bind-error + size-limit** (#28 a,b) — small, self-contained, close a DoS + a silent
   startup failure.
2. **Azure Functions discriminator** (#26) — additive, unblocks real multi-function apps.
3. **API Gateway v2 router** (#25 A) — closes the HTTP-API hard-fail.
4. **Kafka Lambda ordering + partial-batch** (#27) — correctness; gate on the `itemIdentifier` format.
5. **Binary HTTP bodies** (#25 B + #28 c) — the shared cross-cutting body-seam widening, last.

