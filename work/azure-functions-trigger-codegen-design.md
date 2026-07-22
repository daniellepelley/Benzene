# Auto-generating Azure Functions trigger functions — design

**Status:** Design proposal (plan-first). Generalizes the HTTP-trigger investigation to **every**
Azure Functions transport Benzene supports. No code yet — this is the thing to review before building.

**Problem.** Today, hosting Benzene on Azure Functions makes the user hand-write a boilerplate
trigger class per transport — inject `IAzureFunctionApp`, add a `[Function]`/`[XTrigger]` method,
forward to `_app.HandleX(...)`. It's pure ceremony, and (worse) it's the part that must be *exactly*
right for the Functions host to register/dispatch the trigger — the source of the "sync triggers"
deploy failures. We want Benzene to generate these for the user from a small **declaration**.

**The declaration is the key requirement.** We cannot infer binding details — a route (`orders`), a
queue name, an Event Hub name, a Cosmos container — so the user must be able to *declare* each
trigger and its settings. HTTP's `[Function("orders")]` + `Route` is the obvious example: the user
picks the name/route; we can't hard-code `orders`.

---

## 1. How Azure Functions discovery works (the two constraints that shape everything)

Verified against `Microsoft.Azure.Functions.Worker.Sdk` 2.0.7 targets in this repo:

1. **Generated `[Function]` methods + worker indexing don't mix.** The SDK runs, by default
   (`FunctionsEnableWorkerIndexing=true`), Roslyn **source generators** for the in-worker metadata
   provider *and* the function executor. Roslyn generators cannot see each other's output, so a
   `[Function]` emitted by a Benzene generator is invisible to them → the host's `functions.metadata`
   (produced by a *post-compile reflection* MSBuild task, which **does** see generated code) would
   list a trigger the worker can't dispatch. **Fix:** set `FunctionsEnableWorkerIndexing=false`,
   which falls back to the reflection-based metadata *and* executor — both see generated code.
   Benzene ships this in a `buildTransitive` `.props` (in `Benzene.Azure.Function.Core`) so it's
   automatic and overridable. (Cost: reflection at startup instead of source-gen — negligible; it's
   the long-standing pre-indexing default.)
2. **Extension packages must be referenced *directly*.** `docs/azure-functions.md` already documents
   this: the `[XTrigger]` attribute compiles transitively via the Benzene package, but the Functions
   tooling only registers a trigger whose `Microsoft.Azure.Functions.Worker.Extensions.*` package the
   **app** references directly. We **cannot** remove that one csproj line per non-HTTP transport — it
   is a Microsoft tooling constraint, not ours. The generator removes the trigger *class*; the
   extension `PackageReference` stays (and we document/diagnose it).

So the deliverable is: **a source generator that emits the trigger class from a declaration**, plus
the `WorkerIndexing=false` prop. The extension-package reference remains the user's (one line).

---

## 2. The transports (all nine) and what each declaration must carry

Every Benzene Azure Functions transport, its trigger binding, and the dispatch call the generated
function forwards to (all verified in `src/Benzene.Azure.Function.*`):

| # | Transport | Package | Trigger binding attribute | Bound param | Dispatch call | Returns |
|---|---|---|---|---|---|---|
| 1 | **HTTP** | AspNet | `[HttpTrigger(AuthLevel, methods…, Route=)]` | `HttpRequest` | `HandleHttpRequest(req)` | `Task<IActionResult>` |
| 2 | **Service Bus** | ServiceBus | `[ServiceBusTrigger(queue \| topic+subscription, Connection=, IsBatched?, AutoCompleteMessages?)]` | `ServiceBusReceivedMessage[]` (+ opt. `ServiceBusMessageActions`) | `HandleServiceBusMessages(msgs)` | `Task` |
| 3 | **Event Hubs** | EventHub | `[EventHubTrigger(hub, Connection=, ConsumerGroup=)]` | `EventData[]` | `HandleEventHub(events)` | `Task` |
| 4 | **Kafka** | Kafka | `[KafkaTrigger(brokerList, topic, ConsumerGroup=, …)]` | `KafkaRecord[]` | `HandleKafkaEvents(events)` | `Task` |
| 5 | **Cosmos DB** | CosmosDb | `[CosmosDBTrigger(db, container, Connection=, LeaseContainerName=)]` | `IReadOnlyList<TDoc>` | `HandleCosmosDbChanges<TDoc>(docs)` | `Task` |
| 6 | **Queue Storage** | QueueStorage | `[QueueTrigger(queue, Connection=)]` | `string` | `HandleQueueMessage(text)` | `Task` |
| 7 | **Blob Storage** | BlobStorage | `[BlobTrigger(path, Connection=)]` | `byte[]` (+ `{name}`) | `HandleBlob(name, content)` | `Task` |
| 8 | **Event Grid** | EventGrid | `[EventGridTrigger]` | `string` | `HandleEventGridEvent(json)` | `Task` |
| 9 | **Timer** | Timer | `[TimerTrigger(cron, RunOnStartup=)]` | `TimerInfo` | `HandleTimer()` | `Task` |

Two transports need shape beyond a plain name: **Service Bus** (queue *or* topic+subscription, batch),
**Cosmos DB** (generic over the document type — the declaration must carry `typeof(TDoc)`).

---

## 3. The declaration surface — assembly attributes

A source generator runs at compile time, so the declaration must be compile-time-visible. The
idiomatic, generator-friendly mechanism is **assembly-level attributes** — one line per trigger, the
user names each and supplies its binding params. `AllowMultiple = true`, so a service can declare
several triggers of the same kind. This is the "extra configuration the user can do" the requirement
asks for, and it keeps the binding literals exactly where Azure needs them (compile-time constants).

```csharp
// HTTP — the user owns the name AND the route (we never hard-code "orders")
[assembly: BenzeneHttpTrigger(Name = "orders", Route = "{*restOfPath}",
                              AuthorizationLevel = AuthorizationLevel.Anonymous,
                              Methods = new[] { "get", "post", "put", "delete", "options" })]

[assembly: BenzeneServiceBusTrigger(Name = "orders-sb", QueueName = "orders",
                                    Connection = "ServiceBusConnection")]
[assembly: BenzeneServiceBusTrigger(Name = "audit-sb", TopicName = "audit",
                                    SubscriptionName = "svc", Connection = "ServiceBusConnection")]
[assembly: BenzeneEventHubTrigger(Name = "telemetry", EventHubName = "telemetry",
                                  Connection = "EventHubConnection", ConsumerGroup = "$Default")]
[assembly: BenzeneKafkaTrigger(Name = "orders-kafka", BrokerList = "BrokerList",
                               Topic = "orders", ConsumerGroup = "svc")]
[assembly: BenzeneCosmosDbTrigger(Name = "orders-feed", DatabaseName = "shop", ContainerName = "orders",
                                  DocumentType = typeof(OrderDoc), LeaseContainerName = "leases",
                                  Connection = "CosmosDbConnection")]
[assembly: BenzeneQueueTrigger(Name = "orders-q", QueueName = "orders", Connection = "AzureWebJobsStorage")]
[assembly: BenzeneBlobTrigger(Name = "ingest", Path = "incoming/{name}", Connection = "AzureWebJobsStorage")]
[assembly: BenzeneEventGridTrigger(Name = "events")]
[assembly: BenzeneTimerTrigger(Name = "aggregate", Schedule = "0 */1 * * * *", RunOnStartup = true)]
```

**Attribute placement.** Each attribute lives in the transport's Benzene package (e.g.
`BenzeneHttpTriggerAttribute` in `Benzene.Azure.Function.AspNet`), so an attribute is only available
when that package is referenced, and `AuthorizationLevel`-style transport-specific types don't leak
into Core. The single generator matches them all by a shared name convention
(`Benzene*TriggerAttribute`), exactly as the existing `MessageHandlerSourceGenerator` matches
`MessageAttribute` by display string.

Each generated class is the minimal, correct trigger — e.g. for Service Bus:

```csharp
// <auto-generated/>
public sealed class OrdersSbFunction
{
    private readonly IAzureFunctionApp _app;
    public OrdersSbFunction(IAzureFunctionApp app) => _app = app;

    [Function("orders-sb")]
    public Task Run([ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage[] messages)
        => _app.HandleServiceBusMessages(messages);
}
```

---

## 4. Implementation shape

- **One incremental generator**, `Benzene.Azure.Function.SourceGenerators` (netstandard2.0, packaged
  `analyzers/dotnet/cs`, mirroring `Benzene.CodeGen.SourceGenerators`'s csproj exactly). It reads the
  assembly attributes and emits one function class each. A generator that emits nothing when no
  attribute is present is inert — referencing it costs nothing.
- **Attribute types**, one per transport, in each transport's package (so they're available with the
  package and carry transport-specific enums natively).
- **`buildTransitive` props** in `Benzene.Azure.Function.Core` setting
  `FunctionsEnableWorkerIndexing=false` (overridable). This is the load-bearing bit from §1.1.
- **Diagnostics** (the generator's real DX value): if a declared transport's Microsoft extension
  package isn't directly referenced (§1.2), emit a build **warning** (`BENZAF00x`) with the exact
  `PackageReference` to add — turning the opaque runtime "trigger not registered" into a build-time
  nudge. Also error on a duplicate `Name`, and on a Cosmos declaration missing `DocumentType`.

## 5. Validation (mostly local, no Azure needed)

The make-or-break — does a generated `[Function]` reach the host — is checkable at **build time**:
1. Prototype the generator; wire the classic `examples/Azure/Benzene.Example.Azure` to declare its
   HTTP + Queue + Service Bus triggers via attributes and **delete** the three hand-written classes.
2. `dotnet build`, then assert the emitted `obj/.../functions.metadata` still lists `orders`
   (httpTrigger, right route/methods), `orders-queue`, `orders-service-bus` — proving the reflection
   path picks up generated triggers with `WorkerIndexing=false`.
3. Unit-test the generator against the existing `MessageHandlerSourceGeneratorTest` harness (snapshot
   the emitted source per transport).
The executor/runtime half rides on the reflection executor (the proven pre-indexing default);
end-to-end is confirmed by the `AzureFunctionsMesh` deploy once green.

## 6. Rollout

1. Generator + `buildTransitive` prop + **HTTP** attribute; validate via functions.metadata locally.
2. Add the remaining eight attributes + emit branches (mechanical once the pattern holds); snapshot
   tests per transport.
3. Convert `examples/Azure` and `examples/AzureFunctionsMesh` to the declarative form (delete the
   hand-written `HttpFunction`/`QueueFunction`/`ServiceBusFunction`/`AggregateTimerFunction`).
4. Rewrite `docs/azure-functions.md`'s trigger sections around the declarations; keep the hand-written
   form documented as the escape hatch (the generator only *adds* a path — a user can still write a
   `[Function]` by hand, and both coexist).

## 7. Open questions

1. **Zero-config HTTP?** Emit a default catch-all HTTP trigger just from referencing the AspNet
   package (name = assembly name), overridable by `[assembly: BenzeneHttpTrigger(...)]`? Or always
   require the attribute? (Non-HTTP always needs an attribute — they have no sane default binding.)
2. **Auto-set `WorkerIndexing=false`?** Ship it in `buildTransitive` (zero-touch, but Benzene changes
   a global build prop) vs. document it + emit a diagnostic if left on. Recommendation: ship it —
   without it the feature is silently broken, which is worse than the small surprise.
3. **Attribute home:** per-transport packages (recommended, no coupling) vs. one
   `Benzene.Azure.Function.Abstractions`. Per-transport keeps `AuthorizationLevel` etc. where they
   belong.
