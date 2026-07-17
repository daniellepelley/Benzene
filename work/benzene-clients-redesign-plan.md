# Benzene.Clients* Outbound Redesign — Concrete Design Proposal (2026-07-17)

**Status:** Design proposal only — no code changes accompany this document. Follows on from
`work/benzene-clients-vision.md`, which "intentionally stops at principles" (§4) and asks for "a
design pass that proposes a concrete shape for the outbound router and pipeline, checked against
section 2 before being checked against any existing code." This document is that pass.

**This is a breaking redesign of public API in `Benzene.Clients`, `Benzene.Clients.Aws`, and
`Benzene.CodeGen.Client`.** Per `AGENTS.md`'s plan-first convention and "do not change public API
signatures... without flagging it as a breaking change," implementation should not start on this
without an explicit go-ahead — this document is the thing to review before that decision, not a
green light to proceed.

**2026-07-17 update — approved and Steps 1-2 implemented.** One correction discovered during Step
2: §2.1's `IBenzeneMessageSender.SendAsync<TRequest,TResponse>(topic, request)` two-arg snippet
turned out to be incomplete — the pre-existing generated client (`Benzene.CodeGen.Client`) already
had a real, public per-call `headers` overload (`XAsync(message, IDictionary<string,string> headers)`),
and migrating it onto the new sender without threading headers through would have silently dropped
that capability. `SendAsync` gained a third, optional `headers` parameter
(`IDictionary<string,string>? headers = null`) and `OutboundContext` gained a matching `Headers`
property (never null, defaults empty) — see `src/Benzene.Clients/CLAUDE.md` for the shipped shape.
Everything else in §2 shipped as designed.

**2026-07-17 update — Step 3 implemented (SQS/SNS; Lambda deferred).** §2.4's middleware-ification
table shipped with one simplification: **retry needed no new type at all** -
`Benzene.Resilience.RetryMiddleware<TContext>`/`.UseRetry<TContext>(...)` already existed, fully
generic, with a `shouldRetryContext: Func<TContext,bool>` predicate - it works on `OutboundContext`
unmodified. `CorrelationIdMiddleware`/`W3CTraceContextMiddleware` shipped as new, small
`IMiddleware<OutboundContext>` types (`Benzene.Clients.CorrelationId`/`Benzene.Clients.TraceContext`),
added via `.UseCorrelationId()`/`.UseW3CTraceContext()`, converted directly from
`CorrelationIdBenzeneMessageClient`/`TraceContextBenzeneMessageClient`'s logic. `HeadersMiddleware`
was never built - unnecessary, since Step 2's per-call `headers` parameter on `SendAsync` already
closes the "ambient mutable header state" concern §2.4 raised, structurally, without a decorator.
`.UseSqs(...)`/`.UseSns(...)` shipped on the outbound pipeline builder (`Benzene.Clients.Aws`), via
new `OutboundSqsContextConverter`/`OutboundSnsContextConverter` (the `OutboundContext` counterparts
of the existing `SqsContextConverter<T>`/`SnsContextConverter<T>`). **`.UseAwsLambda(...)` is
explicitly deferred, not implemented** - SQS/SNS already prove the pattern generically end to end;
Lambda would follow the identical recipe (`OutboundAwsLambdaContextConverter` +
`.UseAwsLambda(functionName, ...)`) whenever it's next picked up. **Real constraint worth knowing**:
since SQS/SNS have no request/response semantics beyond a send acknowledgement, a topic routed
through `.UseSqs(...)`/`.UseSns(...)` must be sent via `IBenzeneMessageSender.SendAsync<TRequest,Void>`
- calling it with any other `TResponse` compiles but throws `InvalidCastException` at runtime
(`OutboundContext.Response` is always boxed as `IBenzeneResult<Void>` for these two transports).
This is a real, inherent trade-off of `IBenzeneMessageSender`'s unconstrained-generic shape (§2.1) -
the old `IBenzeneClientContext<T,Void>`-typed pipelines caught this at compile time; the new one
only catches it at runtime. Not fixed here - flagging for whoever next revisits the interface shape.

**2026-07-17 update — Step 4 complete. All four steps of this design are now implemented and
shipped.** The verified-safe deletion set from the scope-correction note below was deleted in full:
all `Benzene.Clients`/`Benzene.Clients.Aws` types listed there, plus their 11 exclusively-obsolete
test files (deleted wholesale) and one mixed test file (`Aws/Client/ExtensionsTest.cs`, partially
edited to drop only its obsolete-mechanism tests). `docs/migration-alpha-to-1.0.md` gained a
"Breaking: removed the `ClientBuilder`-based outbound client mechanism" section with the full
old→new mapping; `CHANGELOG.md` gained a `**BREAKING:**` entry; `docs/clients.md` (which predates
this redesign and is written entirely around the deleted mechanism) got a top-of-document notice
pointing at the replacement pending a full rewrite (tracked separately). Local build: 0 errors.
Local `Benzene.Test` suite: 1277/1277 passing (1294 minus the 17 removed obsolete tests, confirming
nothing else broke). `IBenzeneMessageClient` and its concrete transport clients were, as planned,
never touched.

**2026-07-17 update — Step 4 scope correction (deletion not yet performed).** Before starting §4's
deletion pass, a repo-wide grep for every type named in §4's list turned up a materially different
blast radius than this document assumed, and one genuinely dangerous near-mistake worth recording:

- **`IBenzeneMessageClient` itself, and its concrete transport implementations, are NOT part of the
  old mechanism and must NOT be deleted.** `SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`,
  `AwsLambdaBenzeneMessageClient`, `EventBridgeBenzeneMessageClient` (all `Benzene.Clients.Aws`),
  `GrpcBenzeneMessageClient` (`Benzene.Grpc.Client`), and `KafkaBenzeneMessageClient`
  (`Benzene.Kafka.Core`) all implement `IBenzeneMessageClient` directly and are load-bearing,
  actively-used clients for packages entirely outside this redesign's scope. §4's original list
  (`ClientsBuilder`/`SingleClientsBuilder`/`IBenzeneMessageClientFactory`/`IClientMessageRouter`/
  `IDependencyWrapper<T>`/`DependencyWrapperFactory<T>`/decorator-wrapper classes) never named
  `IBenzeneMessageClient` explicitly, but a naive "delete everything the obsoleted factory layer
  touches" pass would have reached it transitively and broken gRPC, Kafka, EventBridge, and Mesh
  clients. It stays.
- **Step 1 left a gap in `Benzene.Clients.Aws`** that's now closed (this update, non-breaking):
  `SqsBenzeneMessageClientFactory`, `AwsLambdaBenzeneMessageClientFactory`,
  `SqsBenzeneMessageClientExtensions`, `AwsLambdaBenzeneMessageClientExtensions`, and
  `Extensions.AddBenzeneMessageClients`/`AddBenzeneMessageClient`/`AddLambdaClients` all consumed
  `IBenzeneMessageClientFactory`/`ClientsBuilder`/`SingleClientsBuilder` but were never themselves
  marked `[Obsolete]` in Step 1 - only the `Benzene.Clients`-core aggregation types were. Also newly
  marked `[Obsolete]`, confirmed (via grep) to be exclusively reachable through that same obsoleted
  factory/builder layer and nothing else: `ClientBuilder`, `BenzeneMessageClientFactory`,
  `RetryBenzeneMessageClient`, `HeaderBenzeneMessageClient`, `HeadersBenzeneMessageClient`,
  `CorrelationIdBenzeneMessageClient`, `TraceContextBenzeneMessageClient`,
  `RetryBenzeneMessageClientWrapper`, `CorrelationIdBenzeneMessageClientWrapper`,
  `TraceContextBenzeneMessageClientWrapper`, `ClientMessageSender<TRequest,TResponse>`,
  `ClientMapping`, `ClientMappingBuilder`, `TopicAndServiceKey`, `IClientHeaders`, `ClientHeaders`
  (all `Benzene.Clients`).
- **The verified-safe Step 4 deletion set** is therefore: everything listed in the two bullets
  above (the aggregation/factory/router/decorator layer, now fully `[Obsolete]` across both
  `Benzene.Clients` and `Benzene.Clients.Aws`) - and nothing else. `IBenzeneMessageClient`,
  `ClientExtensions` (its generic `SendMessageAsync` sugar, unrelated to the factory layer), and
  every concrete `*BenzeneMessageClient` transport implementation stay.
- **Test-file impact still needs a file-by-file pass before the actual deletion commit** - some
  test files (e.g. `test/Benzene.Core.Test/Clients/Aws/Lambda/AwsLambdaBenzeneMessageClientTest.cs`,
  `test/Benzene.Core.Test/Clients/Aws/Sqs/SqsBenzeneMessageClientTest.cs`) test the *surviving*
  concrete client classes and must be kept; others (e.g. `AwsLambdaBenzeneMessageClientFactoryTest.cs`,
  `BenzeneMessageClientFactoryTest.cs`, `ClientExtensionTest.cs`, `HealthCheckTest.cs`'s
  retry-client cases) test only the deleted factory/decorator layer and go with it. Not yet
  itemized file-by-file - the next work session on Step 4 should do that enumeration before
  deleting anything, per this document's own "confirm via grep before deleting, not by assumption."

## 1. Current shape (verified against actual code)

- `IBenzeneMessageClient.SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest>)` —
  the real, low-level send. Fine as-is; not what this redesign touches.
- `IBenzeneMessageClientFactory.Create()` / `.Create(string service, string topic)` — every AWS
  factory ignores both arguments of the second overload since it only ever has one client.
- `IClientMessageRouter.GetClient<TRequest>()` — a second, type-keyed routing concept, unrelated to
  the factory's string-keyed one.
- `ClientsBuilder`/`SingleClientsBuilder` — split by cardinality (`ClientsBuilder` has 20+ lines of
  commented-out dead code from a previous attempt at this exact unification).
- `IDependencyWrapper<T>`/`DependencyWrapperFactory`/`ClientBuilder.WithRetry()` — a decorator
  system parallel to, and inconsistent with, the framework's own middleware pipeline (confirmed:
  `SqsClientMiddleware`/`SnsClientMiddleware`/`HttpClientMiddleware` already exist as ordinary
  `IMiddleware<TContext>`, so decoration-as-middleware isn't hypothetical — it's how outbound
  transport calls themselves already work; only cross-cutting concerns bypass it).
- `Benzene.CodeGen.Client`'s `MessageClientSdkBuilder`-generated methods call
  `_clientFactory.Create(serviceName, topic).SendMessageAsync<TRequest,TResponse>(...)` — i.e. the
  generated, spec-driven client (the vision doc's §2.5 "primary developer-facing surface") already
  exists and works, but its method bodies sit on top of the exact mechanism being replaced.

## 2. Proposed shape

### 2.1 `IBenzeneMessageSender` — the one interface business logic depends on

```csharp
namespace Benzene.Clients;

public interface IBenzeneMessageSender
{
    Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(string topic, TRequest request);
}
```

No service name, no client type, no factory resolution at the call site — satisfies the vision's
§2.1 design-decision test directly. `Benzene.CodeGen.Client`'s generated methods become:

```csharp
public Task<IBenzeneResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request)
    => _sender.SendAsync<CreateOrderRequest, CreateOrderResponse>("order:create", request);
```

— one dependency (`IBenzeneMessageSender`), not `IBenzeneMessageClientFactory` plus a
service-name/topic pair to remember correctly.

### 2.2 `OutboundRoutingBuilder` — one topic-keyed table, one cardinality

Replaces `ClientsBuilder` and `SingleClientsBuilder` outright — "one client" becomes the N=1 case of
"many," per the vision's §2.4 test:

```csharp
services.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline.UseSqs(queueUrl).UseRetry(3).UseW3CTraceContext())
    .Route("payment:charge", pipeline => pipeline.UseHttp(paymentServiceBaseUrl).UseRetry(2))
    .Route("audit:log", pipeline => pipeline.UseSns(topicArn)));
```

`.Route(topic, Action<IOutboundPipelineBuilder>)` builds an ordinary `IMiddlewarePipeline<OutboundContext>`
per topic — `OutboundContext` wraps the outgoing `IBenzeneClientRequest<TRequest>` plus a settable
response slot, mirroring how inbound `TContext` types work today. `UseSqs`/`UseSns`/`UseHttp`
become the outbound-pipeline equivalents of the existing `SqsClientMiddleware`/etc. — same
middleware, wired through `.Use(...)` like every other pipeline in the framework, not a special
case.

**Startup validation** — `OutboundRoutingBuilder.Build()` runs at DI-container-build time and
throws `DuplicateOutboundRouteException` for a repeated topic (replacing `ClientsBuilder`'s
existing but easy-to-miss `ArgumentException` with a named, purpose-specific type). It does *not*
attempt to validate "every topic a handler might send on has a route" — that's not statically
knowable from the routing table alone (see §3, "what this does not solve," for how §2.5 closes that
gap instead).

### 2.3 `DefaultBenzeneMessageSender` — the resolver

```csharp
internal class DefaultBenzeneMessageSender : IBenzeneMessageSender
{
    private readonly IReadOnlyDictionary<string, IMiddlewarePipeline<OutboundContext>> _routes;
    private readonly IServiceResolver _resolver;

    public async Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(string topic, TRequest request)
    {
        if (!_routes.TryGetValue(topic, out var pipeline))
        {
            throw new UnroutedTopicException(topic);
        }
        var context = new OutboundContext(topic, request);
        await pipeline.HandleAsync(context, _resolver);
        return (IBenzeneResult<TResponse>)context.Response;
    }
}
```

`UnroutedTopicException` (not the current generic `InvalidOperationException`) is the runtime
fallback for a genuinely unrouted topic — expected to be rare once §2.5's compile-time path is the
norm, not the primary safety net.

### 2.4 Decoration is middleware, full stop

`IDependencyWrapper<T>`, `DependencyWrapperFactory`, and `ClientBuilder.WithRetry()` are deleted.
Their current decorators become ordinary outbound middleware:

| Today | Becomes |
|---|---|
| `RetryBenzeneMessageClient` (hand-nested or via `IDependencyWrapper<T>`) | `RetryMiddleware<OutboundContext>`, added via `.UseRetry(n)` |
| `HeadersBenzeneMessageClient`/`IClientHeaders` (ambient `Set`/`Get` dictionary) | `HeadersMiddleware<OutboundContext>`, headers passed explicitly per `.Route(...)` call, not injected as mutable ambient state — closes the vision's §1 "ambient mutable header state" concern directly |
| `CorrelationId/` decorator (`WithCorrelationId()`) | `CorrelationIdMiddleware<OutboundContext>`, added via `.UseCorrelationId()` |
| `TraceContext/` decorator (`WithW3CTraceContext()`) | `W3CTraceContextMiddleware<OutboundContext>`, added via `.UseW3CTraceContext()` |

Every row is a rename/relocation of already-correct logic into the middleware shape, not new
behavior — this table is the actual migration checklist for that half of the work.

### 2.5 `Benzene.CodeGen.Client` gets a compile-time safety net

Per the vision's §2.5, the generated client is meant to be the default surface, with the raw
`IBenzeneMessageSender.SendAsync(topic, ...)` call as the low-level escape hatch. To make a missing
outbound route a build-or-startup failure instead of a runtime miss the first time a rarely-hit code
path executes, `MessageClientSdkBuilder` additionally emits one static method per generated client:

```csharp
public static class OrderServiceClientRouting
{
    public static readonly string[] RequiredTopics = { "order:create", "order:cancel" };
}
```

A single `app.ValidateOutboundRouting()` call (added once, typically right after
`AddOutboundRouting(...)`) reflects over every registered `*Routing.RequiredTopics` array in the
loaded assemblies and throws a single aggregate exception listing every missing topic, at startup —
not per-generated-client-call-site plumbing, and not a hard requirement (an app with no generated
clients, or that prefers the runtime `UnroutedTopicException` fallback, simply doesn't call it).

## 3. What this does not solve (explicit, matching the vision's own §3 "deliberately doesn't")

- **No reflection-discovered routing.** The topic → transport binding is external configuration
  (which SQS queue, which base URL) and stays explicitly authored, per the vision's own §3.
- **No retroactive validation of hand-typed `SendAsync(topic, ...)` call sites** that don't go
  through a generated client — by definition, a raw string topic used directly in business logic
  has no static list of "required topics" for `ValidateOutboundRouting()` to check. This is the
  precise cost of using the escape hatch instead of the generated surface, called out directly by
  the vision's §2.5 design-decision test.
- **No decision here on `Benzene.Clients.Aws`'s exact `.UseSqs(...)`/`.UseSns(...)`/`.UseAwsLambda(...)`
  outbound-middleware signatures** — these should mirror the existing inbound `UseSqs`/`UseSns`
  extension shapes (topic-routed pipeline builder, `Action<TOptions> configure = null`) established
  this session for inbound failure-handling, but the exact parameter lists are implementation
  detail, not a design-level decision this document needs to make.

## 4. Migration plan (if/when approved)

Given `version.txt` is `0.0.2` (pre-1.0, per `work/1.0-readiness-checklist.md`), a hard breaking
cutover is cheaper now than after a real 1.0 tag — but even pre-1.0, a big-bang PR touching three
packages plus every consumer is worse than a staged rollout other engineers/AI sessions can review
incrementally:

1. **Add, don't remove.** Introduce `IBenzeneMessageSender`, `OutboundRoutingBuilder`,
   `OutboundContext`, and the middleware-based decorators in `Benzene.Clients` alongside the
   existing types. Mark `ClientsBuilder`/`SingleClientsBuilder`/`IBenzeneMessageClientFactory`/
   `IClientMessageRouter`/`IDependencyWrapper<T>` `[Obsolete]` with a message pointing at the
   replacement, not deleted yet.
2. **Migrate `Benzene.CodeGen.Client`** to generate against `IBenzeneMessageSender` (plus the
   `RequiredTopics`/`ValidateOutboundRouting()` pair from §2.5). This is the highest-leverage single
   change, since it's the primary developer-facing surface per the vision's §2.5.
3. **Migrate `Benzene.Clients.Aws`** onto route-registration extension methods
   (`.UseSqs(...)`/`.UseSns(...)`/`.UseAwsLambda(...)` on the outbound pipeline builder), matching
   naming symmetry with the inbound side.
4. **Delete the obsoleted types** once nothing in `src/`/`examples/`/`test/` references them — a
   real breaking change at that point, documented in `CHANGELOG.md` under `**BREAKING:**` and
   `docs/migration-alpha-to-1.0.md`, matching the precedent already set for other pre-1.0 breaking
   renames this session and prior ones.

Each of the four steps above is independently committable/testable/CI-verifiable, following the
same incremental-with-CI-gate workflow used for every other change in this session — none of them
needs to land as one giant PR.

## 5. Open questions for whoever approves this

1. Does `OutboundContext` need to be generic (`OutboundContext<TRequest>`) to avoid boxing/casting
   the response in `DefaultBenzeneMessageSender.SendAsync`, or is the non-generic + cast shown in
   §2.3 acceptable given every existing `IMiddleware<TContext>` in this codebase is non-generic over
   the payload already?
2. Should `ValidateOutboundRouting()` (§2.5) be opt-in (as designed) or should
   `AddOutboundRouting(...)` require it by default, trading a slightly bigger startup-time surface
   for catching more misconfiguration automatically?
3. Is a hard `[Obsolete]`-then-delete migration (§4) preferred over deleting the old mechanism
   outright in one PR, given the project is pre-1.0 and (per `work/1.0-readiness-checklist.md`)
   single-maintainer? The staged approach costs more total diff churn; the one-shot approach costs
   more review surface in a single change. No strong recommendation either way — flagging as a
   genuine judgment call for whoever picks this up.
