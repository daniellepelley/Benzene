# The Modular Monolith, and the Road Out of It

**Status: DRAFT v0.1 — part of the [Patterns](index.md) "composing a system" set.**

Most systems should start life as **one deliverable**: a single process, built, tested, deployed
and rolled back as a unit. The industry keeps re-learning this — the teams who jumped straight to
microservices spent their early years paying what Martin Fowler calls the *microservice premium*
(distributed operations, network failure handling, cross-service refactoring) before they had
product-market fit for their own architecture, while the teams who started with a well-modularized
monolith shipped. Shopify runs one of the world's largest commerce platforms as a **modular
monolith** on purpose: all the code in one deployable, with strictly enforced boundaries between
domains.

The catch has always been the word *enforced*. In an ordinary codebase a "module boundary" is a
method call, and a method call is free: it shares memory, it shares a transaction, it throws
exceptions across the line, and nothing stops the next commit from reaching around the interface.
Teams bolt on tooling (Shopify built Packwerk to lint theirs) because the language gives the
boundary no teeth. And when the day comes to extract a module into its own service, every one of
those free calls becomes a rewrite: a synchronous, exception-throwing, transaction-sharing call
has to become a message over a wire with failure semantics — new code, new shape, new bugs.

This pattern is about building the monolith on Benzene so that **the boundary has teeth from day
one, and extraction is a wiring change instead of a rewrite**. The mechanism is the
[in-process pipeline](../specification/design-principles.md) — rung 1 of the adoption ladder — plus
the topic-addressed [routing table](service-communication.md#the-routing-table): module-to-module
calls are Benzene messages from the first commit, and a message that crosses a process boundary
looks exactly like one that doesn't.

---

## The shape

One process. Inside it, each business domain is a **module**: its own handlers on its own topics,
its own [middleware pipeline](../specification/core-concepts.md), its own data. Modules never call
each other's code — a module reaches another module the only way any Benzene caller reaches any
Benzene service: **by topic**, through the sender, resolved by the routing table.

```
             ┌────────────────────────────────────────────────────────────┐
   HTTP ───► │                       ONE PROCESS                          │
   queue ──► │                                                            │
             │  ┌────────────┐    ┌────────────┐    ┌────────────┐        │
             │  │  ORDERS    │    │  BILLING   │    │  SHIPPING  │        │
             │  │  handlers  │    │  handlers  │    │  handlers  │        │
             │  │  pipeline  │    │  pipeline  │    │  pipeline  │        │
             │  │  own data  │    │  own data  │    │  own data  │        │
             │  └─────┬──────┘    └─────▲──────┘    └─────▲──────┘        │
             │        │ send("billing:charge", …)         │               │
             │        └─────────────────┴─────────────────┘               │
             │        topic-addressed, via the routing table              │
             │        (routes resolve in process — for now)               │
             └────────────────────────────────────────────────────────────┘
```

Three properties make this a *modular* monolith rather than a monolith with folders:

- **The boundary is a message contract.** What crosses the line is `(topic, request)` in and
  `Result<TResponse>` out — serializable payload types, a string [status](../specification/wire-contracts.md),
  errors as values. A module cannot reach around that: there is no shared object graph to mutate,
  no exception type to catch from another module's internals, no return of a live entity someone
  else's ORM is tracking.
- **The call site is already remote-shaped.** `SendAsync(topic, request)` is asynchronous, returns
  a result with failure statuses, and names no destination
  ([address by topic, not by transport](service-communication.md#address-by-topic-not-by-transport)).
  The caller is written, from day one, in the only style that survives distribution.
- **Failure is in the signature.** A cross-module call can come back `not-found`,
  `validation-error`, `service-unavailable` — and the caller handles that on day one, in process,
  where "service unavailable" never actually happens. When the call later crosses a network and it
  *does* happen, the handling code already exists.

This is the same idea Spring Modulith arrived at for the JVM — module boundaries enforced by
convention, modules talking through events that can later be externalized to a broker — with one
difference of degree: in Benzene the message is not just the *inter-module event* mechanism, it is
the **only** shape any handler is ever invoked in, local or remote, so there is no second (direct
method call) idiom to keep out of the codebase.

---

## Building it *(informative, .NET)*

The in-process transport is a shipped package, `Benzene.Clients.InProcess`, and it has the same
explicit, per-topic, opt-in status as SQS or SNS — a fifth transport, not a magic co-location mode.
The inbound side builds a `BenzeneMessageContext` pipeline over the modules' handlers and registers
it as the in-process dispatch target:

```csharp
// The in-process pipeline: the modules' handler assemblies, plus whatever
// cross-cutting middleware the dispatched topics need.
services.AddInProcessMessaging(pipeline => pipeline
    .UseMessageHandlers(
        typeof(ChargeCardHandler).Assembly,       // billing
        typeof(ReserveStockHandler).Assembly));   // shipping
```

The outbound side is the ordinary [routing table](service-communication.md#the-routing-table) —
an in-process route is declared exactly like a queue route:

```csharp
services.AddOutboundRouting(routing => routing
    .Route("billing:charge",  p => p.UseInProcess())
    .Route("shipping:reserve", p => p.UseInProcess())
    .Route("audit:log",        p => p.UseSns(auditTopicArn)));  // and some topics already leave
```

And module-to-module calls go through the sender — the call site names a topic and nothing else:

```csharp
// In an Orders handler. Nothing here says "billing is in this process".
IBenzeneResult<ChargeRaised> charge =
    await sender.SendAsync<ChargeCard, ChargeRaised>("billing:charge", request);
```

Two deliberate semantics make the in-process route a rehearsal for distribution rather than a
shortcut around it: each dispatch runs in its **own fresh DI scope** (the isolation a real
cross-process call would have), and the payload is **serialized by default** (same validation,
casting, and versioning middleware as any wire transport; no shared mutable object sneaking across
by reference).

Because every module in the process consumes messages the same way, the monolith is not confined
to rung 1 of the adoption ladder: the same process can mount an HTTP adapter for the Orders
module's public API and a queue consumer for inbound events, all dispatching into the same
pipelines. "Monolith" here describes the **deployment unit** — one deliverable — not the number of
transports it serves.

> **What ships today, precisely** *(informative, .NET)*: `Benzene.Clients.InProcess` ships
> `AddInProcessMessaging` (inbound) and `.UseInProcess()` (outbound) with typed request/response
> through the ordinary `SendAsync`, alongside the router's existing `UseSqs`/`UseSns`. Current
> limits, scoped as follow-on work (`work/inprocess-modular-monolith-scope.md` in benzene-dotnet):
> **one in-process pipeline per runtime** — all modules' handlers register in the one
> `AddInProcessMessaging` call, so per-module *middleware stacks* aren't expressible yet (named
> per-module pipelines are the scoped fix); an in-process route to a topic with no handler is
> caught at **first send** (an honest `not-found` result), not at startup; and there is **no
> in-process event fan-out** yet, so one-event-many-consumers choreography starts at extraction.
> None of these limits touch the seam itself — topic-addressed calls through the routing table —
> which is what extraction depends on.

---

## The rules that keep the seams real

The message boundary gives the module seam teeth, but a seam is only as real as the discipline
behind it. These rules cost almost nothing while everything is one process, and they are the
entire difference between "extraction is a wiring change" and "extraction is a rewrite":

1. **Modules only talk by topic.** No module references another module's internals — not its
   handlers, not its entities, not its repositories. If module A needs something from module B,
   there is a topic for it. (A shared `Contracts` package per module — request/response payload
   types only — is the one thing a caller may reference.)
2. **Share-nothing data, from day one.** Each module owns its tables (or schema, or database);
   no other module reads them, even though, in one process, it could. This is the
   [core services](core-services.md) data rule applied early, and it is the rule most worth
   enforcing in review: **a shared table is the one coupling a routing table cannot fix later.**
   Extraction splits compute for free; it does not split a join.
3. **Payloads are messages, not objects.** Everything crossing a module boundary must survive
   serialization — no live entities, no delegates, no "I'll just pass the DbContext". If it can't
   be JSON, it can't cross.
4. **Results, not exceptions.** Domain failure crosses the boundary as a non-success
   [status](../specification/wire-contracts.md), never as a thrown exception type the caller has
   to reference. (Inside a module, throw whatever you like.)
5. **Consumers are idempotent, eventually.** In process, a send is exactly-once; on a queue it is
   at-least-once. Writing new consumers idempotently from the start (or at least flagging the ones
   that aren't) is cheap insurance; retrofitting idempotency under production duplicate traffic is
   not. The [transactional outbox](transactional-outbox.md) picks this thread up.
6. **Version topics deliberately.** Topic and payload [versioning](../specification/versioning.md)
   works identically in process, and starting with it means extraction never has to introduce it.

Rules 1, 3 and 4 are largely **enforced by the shape** — the pipeline only accepts messages and
only returns results. Rules 2, 5 and 6 are yours to keep; Benzene makes them natural, not
automatic.

---

## When to split — organizational signals, not fashion

Nothing in this pattern says you *should* split. A modular monolith with real seams is a
destination in its own right — Shopify has stayed in one for a decade at extraordinary scale — and
the research on team scaling consistently finds the split pays off on **organizational** grounds,
not technical ones. Split a module out when:

- **Deployment contention**: teams queue behind each other's releases; a rollback of one domain
  rolls back another's feature. Independent deployability is the first and best reason.
- **Team ownership**: a module has a dedicated team that wants its own on-call, its own release
  cadence, its own pace — Conway's law is pulling the architecture apart anyway, and it is
  cheaper to follow it deliberately than resist it accidentally.
- **Divergent scaling or runtime needs**: one module needs 50 instances or a GPU or a different
  memory profile while the rest idle; or one module's spikes starve the others in-process.
- **Isolation**: a module's blast radius (a memory leak, a crash loop) or its compliance boundary
  (PCI, data residency) justifies a process of its own.

And do *not* split because the module count "should" be higher: every call that leaves the process
trades nanoseconds for milliseconds and gains a new failure mode. A fleet of services that must
deploy together is a **distributed monolith** — the costs of distribution with none of the
autonomy, strictly worse than the honest monolith you started with.

---

## The extraction: a strangler fig, one wiring change at a time

When a module earns its own process, the move is the
[strangler fig](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/strangler-fig.html)
— extract incrementally, run both, let the monolith shrink — and on a Benzene monolith each
increment is deliberately small:

**1. Stand the module up as a service.** The module's handlers, middleware, and contracts move (or
just compile) into their own deployable, behind whichever transport adapter fits — SQS consumer,
Lambda, HTTP host. The handlers do not change: they never saw a transport in the monolith and they
don't now. This is the same write-once-host-anywhere property that lets any Benzene service
[change platforms as a deployment decision](../specification/transport-bindings.md).

**2. Repoint the routing table.** The monolith's route for that module's topics changes from the
in-process route to the transport route:

```csharp
// Before — resolved inside the process:
.Route("billing:charge", p => p.UseInProcess())
// After — the same topic, now a queue away:
.Route("billing:charge", p => p.UseSqs(billingQueueUrl).UseRetry(3))
```

**No call site changes.** Every caller said `SendAsync("billing:charge", …)` before and says it
after. The routing table was always the one place addresses became destinations
([service communication](service-communication.md)); extraction is the payoff for that discipline.

**3. Run both, then retire one.** During the migration window the extracted service and the
in-process module can both exist — route a topic at the new service while the old module still
handles in-process traffic from a canary, compare, then delete the module from the monolith. Each
module extracted this way shrinks the monolith; the monolith itself is just another Benzene
service in the growing fleet, not a legacy artifact.

**4. Let the fleet tooling catch up with you.** The moment the extracted service is deployed it
can climb the rest of the [adoption ladder](../specification/design-principles.md): health checks,
a derived [spec](../specification/wire-contracts.md), the [mesh](../specification/mesh.md). The
service map that was implicit inside one process — who calls whom, over which topics — becomes the
mesh's explicit, observed topology. You do not lose visibility by distributing; on a Benzene
fleet, distributing is when the fleet-level visibility *starts*.

```
   Phase 0                    Phase 1                       Phase n
 ┌───────────────┐        ┌───────────────┐  ┌─────────┐    ┌────────┐ ┌─────────┐ ┌──────────┐
 │ orders billing│        │ orders        │  │ billing │    │ orders │ │ billing │ │ shipping │
 │ shipping      │  ───►  │ shipping      │──►  (SQS)  │───►│        │ │         │ │          │
 │ (in-process   │        │ (routes to ──►│  └─────────┘    └────┬───┘ └────┬────┘ └────┬─────┘
 │  routes)      │        │  billing)     │                      └───── mesh ────┴──────┘
 └───────────────┘        └───────────────┘                    one service each, observed topology
```

---

## Be honest about the wire

Benzene makes extraction a *code* non-event. It does not — nothing can — make it a *physics*
non-event, and pretending otherwise is how distributed monoliths get built. When a route moves
out of process:

- **Latency is real now.** An in-process dispatch costs microseconds; a queue hop or Lambda invoke
  costs milliseconds. A call pattern that was fine at in-process cost (an N+1 fan-out of sends in
  a loop) may be a design smell at network cost. The routing table tells you exactly which topics
  are about to get slower — read it before you flip it.
- **Delivery semantics change.** In-process is exactly-once; queues are at-least-once. Rule 5
  above (idempotent consumers) stops being insurance and starts being load-bearing. The
  [transactional outbox](transactional-outbox.md) is the companion pattern for the publishing
  side.
- **The shared transaction is gone.** In one process, two modules *could* commit in one database
  transaction — and if you let them (against rule 2), that coupling now surfaces as a consistency
  bug. Multi-module writes that must be all-or-nothing become [sagas](orchestrators.md); reactions
  that don't need atomicity become [choreographed events](choreography.md).
- **Partial failure arrives.** `service-unavailable` was a status your caller handled in theory;
  now it happens. Retries, timeouts, and backoff move from the routing table's decoration
  (`UseRetry`) to something you actively tune.

The honest claim, then, is not "distribution becomes free." It is: **the parts of distribution
that are usually a rewrite — call sites, failure handling, message contracts, serialization — were
done on day one, so what remains at extraction time is the part that was always going to be real
work: data separation, idempotency, and operational tuning.** A team that also kept rules 2 and 5
has pre-paid most of that too.

---

## Checklist

The monolith is extraction-ready when:

- [ ] Every cross-module call is a **topic-addressed send** — no module references another's code.
- [ ] Each module's **data is share-nothing**; no cross-module reads or joins, even in-process.
- [ ] Everything crossing a boundary **survives serialization**; failures cross as **result
      statuses**, not exception types.
- [ ] Routing is declared in **one table**, validated at startup — so extraction is one route edit.
- [ ] Consumers of queue-bound-someday topics are **idempotent** (or flagged where not).
- [ ] Splits are argued from **organizational signals** (deploy contention, team ownership,
      scaling, isolation) — not module count.
- [ ] After extraction, the new service climbs the ladder: health, spec, **mesh**.

---

## Further reading

The external thinking this pattern distills:

- [Monolith First](https://martinfowler.com/bliki/MonolithFirst.html) and the
  [Microservice Premium](https://martinfowler.com/bliki/MicroservicePremium.html) — Martin Fowler
  on why systems that started distributed struggle, and what the premium buys (and costs).
- [Deconstructing the Monolith](https://shopify.engineering/deconstructing-monolith-designing-software-maximizes-developer-productivity)
  — Shopify on running commerce-scale traffic in a modular monolith with enforced boundaries.
- [Strangler fig pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/strangler-fig.html)
  — the incremental-extraction playbook this pattern's migration steps follow.
- [Spring Modulith](https://docs.spring.io/spring-modulith/reference/events.html) — the JVM
  ecosystem's convergent answer: enforced module boundaries plus events that externalize to a
  broker when a module leaves the process.

Back to the [pattern overview](index.md).
