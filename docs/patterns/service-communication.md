# Service Communication

**Status: DRAFT v0.1 — part of the [two-tier pattern](two-tier-architecture.md).**

[Orchestrators](orchestrators.md) drive processes by calling [core services](core-services.md), and
core services never call each other. This document is about those calls: how a caller *addresses* a
service, how an address becomes a concrete *destination*, and — on AWS — how the call is *realized*
as Lambda-to-Lambda. It also covers the **central-routing-lambda** options and their cost/latency
trade-offs, because getting inter-service routing right is what keeps this architecture fast and
cheap.

---

## Address by topic, not by transport

The application-level rule: a caller says **what** it wants, by topic, and nothing about **where**
or **how**.

*(informative, .NET)* The call site is just a topic and a request:

```csharp
// An orchestrator step. No queue URL, no ARN, no function name, no client type here.
IBenzeneResult<TenantCreated> result =
    await sender.SendAsync<CreateTenant, TenantCreated>("tenant:create", request);
```

`SendAsync(topic, request)` returns the same `Result<TResponse>` a handler returns, so a saga step's
`Do(...)` is exactly this call and needs no adapter. The response type drives the delivery
semantics: a real response type means **request/response**; a `Void` response means
**fire-and-forget** (the caller does not wait).

Keeping destinations out of the call site is what makes the two tiers composable: an orchestrator is
written against topics its saga needs, and *where* `tenant:create` lives — which queue, which
Lambda, which region — is a deployment concern resolved by the routing table, not a line of process
logic.

---

## The routing table

A **routing table** maps each outbound topic to a concrete destination (a queue, a topic/ARN, a
Lambda function, an HTTP endpoint). It is the one place addresses turn into destinations.

*(informative, .NET)* The table is declared once, at startup, in code:

```csharp
services.AddOutboundRouting(routing => routing
    .Route("tenant:create", pipeline => pipeline.UseSqs(tenantQueueUrl).UseRetry(3))
    .Route("audit:log",     pipeline => pipeline.UseSns(auditTopicArn)));
```

Each `Route` builds a small outbound middleware pipeline for that topic; the pipeline encodes the
transport *and* the destination. At runtime `SendAsync("tenant:create", …)` is a dictionary lookup
by topic string onto that pipeline — an unknown topic is an `UnroutedTopicException`, not a silent
drop.

Three properties make this safe at fleet scale:

- **Declared once, at startup — not at the call site.** Destinations live in one wiring block per
  service, not scattered through the process logic. Change where a topic lives by editing the table,
  not the callers.
- **Validated at startup, not on first call.** Generated clients declare the topics they require
  (an `OutboundRoutingContractAttribute` carrying a `RequiredTopics` list), and a startup check fails
  the service immediately if a required topic has no route — a `MissingOutboundRoutesException` at
  boot, not a 3am `UnroutedTopicException` in production. A missing route is a deploy-time error.
- **Resolved at runtime only as a lookup.** The expensive decision (which destination) is made once
  at startup; per-call cost is a dictionary read.

> **What ships today, precisely** *(informative, .NET)*: the outbound router ships transport
> middleware for **SQS and SNS** (`UseSqs`, `UseSns`) — i.e. queue/event delivery is routed by topic
> today. The **request/response Lambda** path (below) is currently a *per-function client* rather
> than a topic-routed transport; wiring Lambda request/response in behind the same
> `AddOutboundRouting(...)` table is the natural next step, and the pattern is written to that goal.
> Treat "everything is addressed by topic" as the target shape; today SQS/SNS reach it through the
> router and Lambda request/response reaches it through the client just below.

---

## The AWS realization: Lambda-to-Lambda

On AWS, the reference realization for an orchestrator→core call is a **direct Lambda invocation**.
It suits this architecture unusually well:

- **Fast.** A warm Lambda-to-Lambda `RequestResponse` invoke is single-digit-millisecond overhead —
  far below an API Gateway or load-balancer hop — so an orchestrator can make several core calls
  inside one API request budget.
- **Cheap.** No gateway, no always-on compute, no idle queue pollers between the tiers. You pay for
  the callee's runtime and the invoke, nothing standing.
- **A natural fit for the Benzene envelope.** The whole call is one `{topic, headers, body}` envelope
  in and one `{statusCode, headers, body}` out — the exact wire contract
  ([wire-contracts.md](../specification/wire-contracts.md)) — so the callee routes it through
  its normal pipeline with no special edge, and the caller gets a typed `Result<TResponse>` back.

*(informative, .NET)* The client wraps `Lambda.Invoke` and picks the invocation type from the
response shape:

```csharp
// A response type → InvocationType.RequestResponse (synchronous, typed result)
IBenzeneResult<TenantCreated> created =
    await lambdaClient.SendMessageAsync(new BenzeneClientRequest<CreateTenant>("tenant:create", req));

// A Void response type → InvocationType.Event (fire-and-forget)
await lambdaClient.SendMessageAsync(new BenzeneClientRequest<AuditLog>("audit:log", entry)); // TResponse = Void
```

The message's `topic` travels **inside** the envelope for the *callee* to route on — the topic does
not choose the destination Lambda. Which function to invoke is a separate question, and it is the
crux of everything below.

---

## The routing problem, and the central-routing-lambda options

Direct Lambda-to-Lambda has one cost: **the caller must know the callee's function name.** A topic
(`tenant:create`) is not a function name (`prod-tenant-service`); something has to bridge the two.
There are three broad answers, and the difference between them is where and when the topic→function
binding is resolved.

### Option 0 — Bind it in the caller (no routing lambda)

The caller holds the topic→function map itself and invokes the target directly. This is what the
[routing table](#the-routing-table) does: the binding is **declared in the caller's wiring** and
resolved locally.

- **Latency/cost:** one invoke. The floor. Nothing to beat.
- **Binding time:** compile-time (in code) or startup (from config) — see
  [Where the binding lives](#where-the-binding-lives).
- **Trade-off:** every caller must know the map. Fine when the map is small and changes rarely;
  it becomes a distribution problem when hundreds of services each need the current table.

This is the default and, for most fleets, the right answer. The two "routing lambda" options exist
for when you want the map to live in **one** place instead of every caller.

### Option A — Routing lambda in the path (the double hop)

A central routing Lambda receives *every* inter-service message, looks up the destination, forwards
the message, waits for the response, and returns it to the caller.

```
  caller ──▶ routing lambda ──▶ target lambda
  caller ◀── routing lambda ◀── target lambda        (two round trips, in series)
```

- **Latency/cost:** roughly **double.** The caller waits for the routing Lambda, which waits for the
  target Lambda — and on AWS you **pay for the routing Lambda's runtime while it blocks on the
  target's**. You have added a synchronous hop *and* a second billed, waiting execution to every
  call.
- **Upside:** the map lives in exactly one place; callers know only "send to the router." Routing
  logic (versioning, canarying, tenant-based routing) is centralized.
- **When it's acceptable:** low call volumes, or when centralized routing policy is worth the
  doubled per-call cost. For the hot path of a high-volume orchestrator, it usually is not.

### Option B — Routing lambda returns the name (resolve, then call direct)

The central routing Lambda is a **directory**, not a relay: the caller asks it "what is the function
for `tenant:create`?", gets a **name** back, and then invokes the target **directly**. The router is
never in the message path.

```
  caller ──▶ routing lambda            "tenant:create?"
  caller ◀── routing lambda            "prod-tenant-service"
  caller ─────────────────▶ target lambda    (direct; router not involved)
```

- **Latency/cost:** the resolve is a tiny call returning a string, and — critically — its result is
  **cacheable**. Resolve once, cache the name, and every subsequent call is a single direct invoke
  at Option-0 speed. Amortized, you approach one invoke per call while keeping the map central.
- **Upside:** central map *and* direct calls. The best of both — the directory is authoritative, but
  it is out of the hot path after the first lookup.
- **Trade-off:** the caller now has a cache to populate and invalidate. Which leads to the real
  question:

### Where the binding lives

Option B's cache, and Option 0's map, can be populated at three different times — a spectrum from
most-static to most-dynamic:

| Bound at | How | Changes require | Best when |
|---|---|---|---|
| **Compile time** | The topic→destination map is code (the `AddOutboundRouting` table). | A rebuild + redeploy. | The map is stable. **This sounds heavier than it is** — builds are cheap and routine, and a compiled map is validated at startup and impossible to typo into a runtime surprise. |
| **Startup / config** | The map is configuration (env, Parameter Store, a config file) read once when the service boots. | A restart / redeploy of config. | You want to retarget without a code change (blue/green, per-environment endpoints). Still resolved once, still validated at boot. |
| **Runtime** | The map is fetched from the routing Lambda (Option B) on first use and cached; or loaded into a routing table at deploy time and refreshed. | Nothing — the directory is the source of truth; callers pick up changes as their cache refreshes. | The fleet is large or changes often and you cannot redeploy every caller to move one service. |

The three are not exclusive. A common, robust shape is **compile-time or startup binding as the
default**, with an **Option-B routing Lambda as the dynamic fallback** for topics not in the local
table — static and fast for the stable core, dynamic for the parts that move.

> **What ships today, precisely** *(informative, .NET)*: Benzene's built-in outbound routing binds
> **in the caller at startup, from code** (`AddOutboundRouting`), validated at boot and resolved at
> runtime as a topic lookup — i.e. **Option 0 with compile-time/startup binding**. A central routing
> Lambda (Option A or B) is an *architectural* choice you layer on top; Benzene does not ship one.
> Option B integrates cleanly: a small resolver populates the same topic→destination table the
> `IBenzeneMessageSender` already reads, so the choice of binding-time is a wiring decision, not a
> change to any call site.

### Choosing

- **Default to Option 0 with compile-time or startup binding.** One invoke, validated at boot,
  no moving parts. Most fleets never need more.
- **Reach for Option B (resolve-then-cache) when the map must be central** — many callers, frequent
  topology changes — and you are unwilling to redeploy callers to move a service. Keep the router out
  of the hot path.
- **Avoid Option A (in-path double hop) on high-volume paths.** Its doubled latency and doubled
  billed runtime are rarely worth it; use it only where centralized in-path policy genuinely earns
  the cost, and at volumes where that cost is immaterial.

---

## Delivery semantics recap

- **Request/response** (orchestrator waits for a result): a real `TResponse`. Used for the reads and
  writes a saga step depends on.
- **Fire-and-forget** (no wait): a `Void` response ⇒ `InvocationType.Event` on Lambda, or an
  `UseSqs`/`UseSns` route. Used for emitted events and audit — the things nobody is blocking on.

A saga's forward steps are almost always request/response (the orchestrator must know each succeeded
before proceeding); the events it emits about the finished process are fire-and-forget.

---

## Checklist

Inter-service communication is well-formed when:

- [ ] Call sites use **topics, never transports** — no queue URLs, ARNs, or function names in process
      logic.
- [ ] A **routing table** maps topics to destinations, declared once and **validated at startup**.
- [ ] Request/response vs fire-and-forget is chosen by **response type** (`Void` ⇒ fire-and-forget).
- [ ] On AWS, orchestrator→core calls are **direct Lambda-to-Lambda** on the hot path.
- [ ] If routing is centralized, it is **Option B (resolve-then-cache)**, not an in-path double hop,
      on any high-volume path.
- [ ] The topic→destination **binding time** (compile / startup / runtime) is a deliberate choice,
      defaulting to compile-time or startup.

Back to the [pattern overview](two-tier-architecture.md).
