# Client Health Checks — extend health checks to every client & dependency

Investigation + design for making **every Benzene client ship a non-destructive, low-cost health
check that comes along automatically when the client is configured**, plus a first-class "bring your
own" path for any third-party dependency, feeding the mesh proposition.

This extends `work/health-checks-1.0-review.md` (which already tracks the provider-coverage gaps as
open items T3.1) and `work/service-mesh-roadmap-1.0.md` (which defines but never built the
`TopologyEdgeSource.Structural` producer). Read those two first.

> **Status:** Investigation complete; **first increment SHIPPED** — the consumer-side contract-drift
> check on a dedicated `contracts` topic (PR #32, see §7). Product-owner positions gathered
> (infrastructure, observability, mesh, DX) — all four APPROVE the direction. What remains open: the
> transport-client **auto-wiring** work (Phases 0–4) and the mesh/interop work (Phase 5). The shipped
> increment is orthogonal to and compatible with them (it needs none of the `HealthCheckFinder`
> harvesting; it registers checks explicitly on its own topic).

---

## 1. Goal (the user's vision)

- Every client Benzene ships (SQS, SNS, EventBridge, Lambda, StepFunctions, Azure Service Bus, Event
  Hub, Event Grid, Queue Storage, HTTP, gRPC, Kafka, RabbitMQ, …) comes with a health check.
- **One-to-one** client↔check mapping, wired **automatically** when you configure the client — no
  second, separate registration, no re-typed resource identifier.
- Checks are **non-destructive** and **as low-cost as possible** (polled, cached).
- Users can **build their own** checks for any third-party service (their DB, a partner API, a Kafka
  topic they consume) *easily* — Benzene doesn't have to ship everything, but everything you connect
  to should be checkable.
- Works for **self-hosted / no-HTTP** services (e.g. a Kafka-only consumer), via the reserved
  health-check message topic or a heartbeat push.
- Feeds the **mesh**: extend the health already shown in the Mesh UI into a real per-service
  dependency view.

## 2. Current state (verified)

**Foundations are strong and already mostly in place:**

- `IHealthCheck` = `Type` + `ExecuteAsync()` → `IHealthCheckResult` (`Status` Ok/Warning/Failed,
  `Data`, structured `HealthCheckDependency[]` = `Kind`+`Name`, per-check `Duration`). `Dependencies`
  and `Duration` are **default interface members** — the proven source/binary-safe extension pattern.
- Execution: `HealthCheckProcessor` runs checks concurrently, each wrapped in `TimeOutHealthCheck`
  (default 10s) + `ExceptionHandlingHealthCheck`. Aggregate `IsHealthy` drops only on `Failed`; a
  `Warning` does not. Opt-in `CachingHealthCheckProcessor` (TTL, keyed by the set of check `Type`s).
- Exposure is **transport-agnostic**: `.UseHealthCheck(topic, …)` is message-handler middleware keyed
  on the reserved `"healthcheck"` topic (also `"liveness"`/`"readiness"`). Any transport that routes
  a message with that topic gets a health response back — no HTTP required. An HTTP transport maps the
  result to 200/503; a gRPC bridge maps Failed→Unhealthy, Warning→Degraded.
- No-return-channel transports (Kafka) already have a push path: `Benzene.CloudService/MeshAnnouncer`
  runs checks in-process and pushes results in a `mesh:heartbeat` every ~10s.
- The mesh already threads the full `HealthCheckResponse` verbatim into `MeshServiceSnapshot`
  (pull/artifact plane) and renders per-check dependencies in the Mesh UI.
- Contract-drift is the existing "client health check" precedent: provider publishes a `"schema"`
  check with a contract hash; consumer/mesh compares (`ClientHealthCheckProcessor` → `ClientHashMatch`,
  drift degrades to `Warning`).

**The five real problems:**

1. **No auto-wiring (the core gap).** Client registration (`.UseSns(arn)`, a pipeline-builder
   extension) and health-check registration (`AddSnsHealthCheck(arn)`, an `IHealthCheckBuilder`
   extension) are two separate surfaces that never call each other. Grep confirms **zero** production
   or example call sites wire a client's check; every example hand-rolls a bespoke `IHealthCheck`.
   The resource identifier gets typed twice, in two locations. Guaranteed "later" that never happens.
2. **Coverage gaps.** Have checks: SQS, SNS, Lambda, StepFunctions, Azure Service Bus, HTTP (+
   standalone DynamoDb, EF, Tcp, Disk, Cache, Schema). **Missing:** EventBridge, Event Hub, Event
   Grid, Queue Storage, gRPC, Kafka, RabbitMQ.
3. **Non-destructive violated.** SQS (`SendMessage` ping), Lambda (real invoke), StepFunctions
   (`StartExecution`) are side-effecting — contradicting the hard requirement.
4. **Inconsistent placement.** AWS clients co-locate the check inside the client package; Azure
   Service Bus / HTTP use a separate `Benzene.HealthChecks.*` package; the rest have neither.
5. **No first-class "bring your own" contract.** The factory/builder primitives exist but the
   lowest-ceremony path (`AddHealthCheck(Func<IServiceResolver,IHealthCheck>)` + hand-built result)
   is a class + try/catch + manual result — too heavy; newcomers skip it.

## 3. Design decisions (product-owner consensus)

### 3.1 Auto-wiring mechanism — DI-collection registration harvested by `HealthCheckFinder`
`.UseSns(arn)` (etc.) additionally self-registers an `IHealthCheck` into the DI collection that
`HealthCheckFinder` already sweeps — the same factory lambda `AddSnsHealthCheck` already contains,
just registered from the client extension. A bare `.UseHealthCheck("healthcheck")` (naming **no**
checks) then picks up every client's check automatically. Exposure stays explicit (you choose the
topic/endpoint); the *content* is automatic.

- **Rejected:** a marker interface on client middleware harvested by walking the built pipeline —
  checks run from a DI set, not by reflecting over pipeline internals, and the health pipeline is a
  *separate* topic pipeline from the client pipeline. Registering into DI is the blessed pattern and
  respects the `TContext`-purity convention (no context markers).
- **Plumbing seam to confirm:** the client `Use*` extensions operate on
  `IMiddlewarePipelineBuilder<TContext>`, not `IServiceCollection`. For auto-registration to work
  that builder (or the surrounding app/DI builder) must expose a config-time hook to register into
  the service collection. **If that seam doesn't exist, building it is the actual work** — everything
  else is a lambda that already exists.
- The auto-registered check must **reuse the client's own SDK handle**: capture the instance when
  `.UseSqs(instance)` supplied one, resolve from DI otherwise — else "health check can't resolve
  `IAmazonSQS`" surprises exactly when the client was hand-supplied.
- **Dedup key = `(Type, Name)`** on the `HealthCheckDependency`: two `.UseSns(sameArn)` → one check;
  two `.UseSns(differentArn)` → two distinct checks.

### 3.2 ⚠️ THE one-way door — deep layer, never a probe (unanimous, highest risk) ✅ *implemented*
`HealthCheckFinder` returns **all** DI-registered checks, and every `.Use*Check(…)` harvests via the
finder too. So a naïvely auto-registered dependency check would land in **every** probe — including
liveness and readiness — and a transient downstream blip (SQS throttle, downstream 503) would take
automated action across the whole fleet at once.

**Two distinct hazards, both real:**
- **Liveness** (failure ⇒ restart): a downstream blip flips liveness to 503 and **Kubernetes restarts
  the pod** — a cross-replica restart storm, and restarting never fixes a downstream anyway.
- **Readiness** (failure ⇒ de-route): a dependency check is **shared-fate** — every replica runs the
  same check against the same downstream, so a blip fails **all** replicas' readiness at once and
  Kubernetes pulls **every** pod from the Service. The Service now has **zero endpoints** →
  callers get connection-refused / DNS-level failures instead of a structured 503 with `Retry-After`,
  breaking L7 retries/circuit-breakers and turning a *degradation* into a *total outage*. De-routing
  only helps when some replicas are healthy to shed to; for a shared downstream there are none. (This
  is the well-established "readiness probes considered harmful for shared dependencies" rule; for a
  worker/consumer, readiness is meaningless anyway — no inbound Service traffic to gate.)

**Decision (revised after review):** auto-wired dependency checks belong on the **deep `healthcheck`
layer only** — scraped by monitoring / the mesh inventory (§3.11) / humans, triggering **no** automated
k8s action. They are excluded from **liveness, readiness AND contracts**. Mechanically this is a DI
registration category `IDependencyHealthCheck` (a sub-interface marker, **not** a `TContext` marker, so
no context-purity issue) that only `.UseHealthCheck` harvests. Liveness keeps process-local self-checks
(`MemoryHealthCheck`); readiness keeps instance-local "can *this* pod serve" checks
(`ShutdownReadinessHealthCheck` for drain). A developer who has reasoned that a *specific* dependency is
genuinely safe to gate traffic on can still add it to readiness explicitly
(`.UseReadinessCheck(b => b.AddSqsHealthCheck(...))`) — auto-wiring never does it for them.

> **Note vs. the original plan:** this section first said "readiness, never liveness." Review surfaced
> that readiness itself is unsafe by default for shared downstreams (the cascading-failure path above),
> so the category was renamed `IReadinessHealthCheck` → `IDependencyHealthCheck` and readiness now
> **excludes** it. "Every client ships a check" is still fully delivered — the check is on the health
> report and the mesh — just not wired to an automated-action probe by default.

### 3.3 Non-destructive default + opt-in "active" tier ✅ *implemented*
Fix the three destructive checks to read-only control-plane calls:

| Check | Destructive today | Non-destructive default |
|---|---|---|
| SQS | `SendMessage` ping | `GetQueueAttributes` |
| Lambda | real invoke | `GetFunctionConfiguration` |
| StepFunctions | `StartExecution` | `DescribeStateMachine` |

This makes every check consistent: "prove the resource exists, is reachable, and my credentials can
see it." **Documented trade-off:** a read-only check proves reachability + IAM *visibility*, not that
a write/invoke would succeed (`sqs:GetQueueAttributes` ≠ `sqs:SendMessage`). Acceptable as the polled
default — the false-negative (healthy-but-can't-write) is rarer than the failures reachability catches
(endpoint down, wrong region, resource deleted, credentials expired).

**Preserve** the write-path behavior as an explicit, off-the-poll-path **"active"/"deep"** tier
(`mode: HealthCheckMode.Active`) with a **distinct `Type`** (e.g. `"Sqs.Active"`, so it never shares a
cache key with the reachability check) and the existing `⚠️ Side-effecting` docs. Not deleted, not a
flag on the default check.

### 3.4 Result semantics — criticality-driven Warning vs Failed
Reuse the existing three-state model (no new statuses — mesh condition):

- **`Failed`** (flips `IsHealthy`): dependency unreachable / not-found / bad credentials **and**
  critical to serving traffic.
- **`Warning`** (degraded, still ready): (a) reachable but slow/degraded; (b) unreachable but
  **non-critical**; (c) permission/authorization error — "I lack permission to *probe* this" is not
  "the app is broken" (a least-privilege publisher stays green); (d) config/contract drift (mirrors
  the existing `ClientHealthCheckProcessor` precedent).

Rule of thumb for the guide: *unreachable + critical = Failed; reachable-but-slow = Warning;
unreachable-but-non-critical = Warning; permission-denied = Warning.*

### 3.5 Extend `IHealthCheck` via default interface members (gating prerequisite) ✅ *implemented*
Add, using the same DIM pattern already on `IHealthCheckResult` (zero breaking change to 20+
implementers):

- `string[] Tags => Array.Empty<string>();` — probe routing/filtering (MEL predicate mapping). Note:
  liveness/readiness **probe separation** is done by topic + the readiness registration category
  (§3.2), **not** by a tag — tags are finer filtering on top.
- `bool IsNonCritical => false;` — critical-vs-non-critical (drives §3.4). **Shipped with inverted
  polarity vs the original `IsCritical => true`:** a health-gating flag must fail *safe*, and a
  `default(bool)` from any Moq/DI proxy over `IHealthCheck` is `false`. With `IsNonCritical` that false
  means *critical* (safe); with `IsCritical => true` it would mean non-critical (fail-open) and silently
  neuter every mocked check in the suite. Same capability, safe default.
- `TimeSpan? Ttl => null;` and `TimeSpan? Timeout => null;` — per-check overrides. `Timeout` is honoured
  by `HealthCheckProcessor`; `Ttl` is shape-locked but its consumption is deferred (the caching
  processor caches the *aggregate* per probe, not per check — per-check TTL needs a separate layer).

This resolves the "no tags/timeout/criticality" open item in `work/observability-roadmap-1.0.md` in
one coherent, need-driven stroke. **One-way door — decide before any API freeze.** (Note: the
`ExecuteAsync()` signature stays token-free by prior decision — cancellation rides the scoped
`ICancellationTokenAccessor`.)

### 3.6 Low-cost / cadence
Auto-wired client checks default to running under `CachingHealthCheckProcessor` (TTL ≈ probe
interval, ~10–15s) — uncached fan-out over every dependency at probe frequency is not viable.
Harden the engine for fan-out: honor the scoped cancellation token in the client checks' read-only
SDK calls (so a timed-out check doesn't orphan a background AWS call), and add **single-flight**
(per-key lock) on `CachingHealthCheckProcessor` cache-miss to avoid a stampede fanning out N×.

### 3.7 Placement — co-locate for 1:1 clients
Co-locate the check in the client package wherever a 1:1 client exists (the AWS SDK is already
referenced; the check adds only a `HealthChecks.Core` reference — nearly free). **Auto-wiring
requires co-location** — `.UseSns` can only auto-register `SnsHealthCheck` if it's reachable from the
client package. Resolve the inconsistency toward co-location: **move the Azure Service Bus check into
its client package.** Reserve standalone `Benzene.HealthChecks.*` packages for dependencies with no
shipped client (EntityFramework, generic HTTP ping, Disk, Tcp, Redis-without-a-Benzene-client).

### 3.8 "Bring your own" — a thin throw-based lambda helper (golden path)
Add on `IHealthCheckBuilder`:

```csharp
IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder builder,
    string kind, string name, Func<Task> probe);   // returns = Ok, throws = Failed
```

Golden-path doc example:

```csharp
.UseReadinessCheck(checks => checks
    .AddHealthCheck("Database", "orders-db", async () =>
    {
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();      // returns = healthy, throws = failed
    }))
```

The overload sets `Type = name`, attaches `new HealthCheckDependency(kind, name)`, and wraps in
`InlineHealthCheck` — so structured dependency metadata is populated *without the developer knowing it
exists*, and timeout + exception isolation are free (the processor already wraps every check). Add a
`Func<Task<bool>>` sibling for the no-natural-throw case. Keep `IHealthCheck` classes + `IHealthCheckFactory`
as the "advanced/reusable" tier.

### 3.9 Failure ergonomics — errors that teach ✅ *implemented*
Today `SnsHealthCheck` reports `Data["Error"] = ex.GetType().Name` and withholds the message
(secret-safety — correct). But `AmazonSimpleNotificationServiceException` alone is unactionable.
Include the **non-sensitive structured discriminators** AWS already returns — error code + HTTP status
— while still withholding the free-text message:

```json
"data": { "TopicArn": "arn:…", "Error": "AmazonSimpleNotificationServiceException",
          "ErrorCode": "AuthorizationError", "StatusCode": 403 }
```

`403 / AuthorizationError` turns "something's wrong with SNS" into "it's IAM on `GetTopicAttributes`
for this ARN." Apply the same enrichment across shipped AWS/Azure client checks.

**Shipped:** the classification *policy* lives once in the cloud-agnostic
`Benzene.HealthChecks.Core.HealthCheckError.Classify(type, ex, deps, errorCode?, statusCode?, data?)`
— a 401/403 → `Warning`, everything else → `Failed`, `Data` enriched with the exception type +
`ErrorCode`/`StatusCode` (never the message). Each AWS check does a 2-line extraction of
`(ErrorCode, StatusCode)` off `AmazonServiceException` and calls it (no cloud-SDK dependency reaches
core, no new shared package). Wired into the SNS/SQS/Lambda/StepFunctions checks. Azure Service Bus
still to do (folds in when its check moves to the client package per §3.7).

### 3.10 `ValidateHealthChecks()` — mirror `ValidateOutboundRouting()`, but WARN by default
A parallel validator that cross-references configured outbound routes / known clients against
registered checks' `HealthCheckDependency` set and reports any configured dependency with no check.
**Must default to a logged warning, not a throw** — throwing at startup because a developer hasn't
health-checked their Postgres is an adoption-killing yak-shave (the opposite of
`ValidateOutboundRouting`'s "a broken route is a real bug"). Offer `ValidateHealthChecks(strict: true)`
opt-in fail-fast. The warn line names the resource and the fix. Defer a Roslyn analyzer.

### 3.11 Mesh integration
- **Pull plane, decisively.** The aggregator already carries the full `HealthCheckResponse` incl.
  `HealthCheckDependency[]` in `MeshServiceSnapshot` — an auto-wired client check lands there with
  **no aggregator data-path change**, only new *derivation* + UI. The push heartbeat stays a
  **rollup** (Healthy/Degraded/Unknown) — do not push the full dependency list through it.
- **No new health status** — map client checks into the existing `Warning`/`Unhealthy` the way
  contract-drift does, or it reopens the two-plane vocabulary reconciliation.
- **Ships free now:** a per-service **outbound-dependency inventory with a live signal per edge** —
  real catalog value, pure pull-plane render.
- **Topology graph is gated on a resource-identity join convention.** To turn A→`{Queue, orders-queue}`
  into an A→B *service* edge you must resolve which service owns that resource. That requires the
  **per-topic per-transport binding key deferred in mesh roadmap §10.16/§10.18** — the client check's
  `HealthCheckDependency.Name` must be a stable identifier matching the provider's spec binding, plus
  a `Kind`→transport mapping and a direction rule. **This proposal is the missing
  `TopologyEdgeSource.Structural` producer**, but ship the inventory first and gate the graph on the
  join key.
- **Rollup** gains two derived levels: edge health per `(service, dependency)`, and resource health
  (group edges by resource identity — "orders-queue reported degraded by 2 of 3 dependents" is a
  shared-cause/root-cause signal, and dedups the issue inbox). Point-in-time rollup now; historical
  trends + alerting stay mesh Phase 5.

### 3.12 Self-hosted / Kafka-only
Prefer the **push/heartbeat** path (already settled by the 2026-07-15 multi-transport ruling): a
Kafka-only consumer self-reports health from inside its real processing loop — truer than a probe,
which can ack on a healthy partition while real work stalls on a lagging one. A request/reply-over-Kafka
probe, *if* on-demand freshness is genuinely wanted, is a secondary optional `IMeshServiceSource`
(`KafkaProbeMeshServiceSource`, the Kafka analog of the existing `LambdaMeshServiceSource`) — reuse
Benzene's reply-over-transport primitives + the aggregator's per-service timeout/isolation, loop in
`performance-champion`, and **never** make a Kafka broker a dependency of the standalone `examples/Mesh`
demo (mock it, as Prometheus is mocked).

### 3.13 `Microsoft.Extensions.Diagnostics.HealthChecks` bridge — build now
Once every client emits a check, ASP.NET Core hosts expect `/health` `/health/live` `/health/ready`
with `Healthy/Degraded/Unhealthy`. Benzene's `Ok/Warning/Failed` maps cleanly (the gRPC bridge proves
it). Ship a **one-directional adapter in a separate package** (Benzene checks → MEL, carrying
`Data`/`Dependencies`/`Duration`), reusing the §3.5 tags for MEL's tag-based predicate filtering.
Keep `HealthChecks.Core` pure — no MEL dependency there. Don't also consume arbitrary MEL checks in
this pass.

## 4. Phased implementation plan

> **✅ Shipped ahead of the phases (PR #32, §7):** the consumer-side **contract-drift** check on a
> dedicated **`contracts`** topic (`UseContractsCheck` + `ClientHealthCheck`/`AddContractCheck`). This
> is the CodeGen-client contract half of the vision, off every Kubernetes probe. The phases below are
> the remaining **transport-client auto-wiring** work (SNS/SQS/… contributing a reachability check when
> configured) — independent of the shipped increment.

**Phase 0 — lock the one-way doors (do before any API freeze; small, foundational)** ✅ *done*
- ✅ 0a. Extended `IHealthCheck` with DIMs `Tags` / `IsNonCritical` (see §3.5 for the polarity flip) /
  `Ttl` / `Timeout`. `HealthCheckProcessor` honours per-check `Timeout` (replaces the processor-wide
  timeout) and `IsNonCritical` (downgrades a `Failed` to `Warning`). `Ttl` shape-locked, consumption
  deferred.
- ✅ 0b. Introduced the **dependency registration category** `IDependencyHealthCheck` (a DI service-type
  marker, not a context marker) + `DependencyHealthCheck` wrapper. Decision: separation stays **topic +
  DI-category** (no `Tags`-based separation axis). `IHealthCheckFinder` gained
  `FindDependencyHealthChecks()`; `HealthCheckBuilder.GetHealthChecks(resolver, includeDependencyChecks)`
  selects scope. Harvest matrix: **only** `.UseHealthCheck` (deep layer) **includes** the category;
  `.UseLivenessCheck`, `.UseReadinessCheck` **and** `.UseContractsCheck` **exclude** it (see the §3.2
  revision — readiness is unsafe by default for shared downstreams). The disjointness is structural — a
  check registered under `IDependencyHealthCheck` is not returned by `IEnumerable<IHealthCheck>`.
  *(Originally named `IReadinessHealthCheck`/harvested on readiness; renamed + re-scoped after the §3.2
  review.)*
- ✅ 0c. Confirmed the config-time seam exists (`IMiddlewarePipelineBuilder.Register`) and added the
  `AddDependencyHealthCheck(IBenzeneServiceContainer, factory, dedupKey)` registration hook (in
  `HealthChecks.Core`, so client packages need only their existing `.Core` reference). Dedup by
  `DedupKey` (Phase 1 sets it to the dependency's `(Type, Name)`) in the finder.

**Phase 1 — auto-wire the clients that already have checks (highest ROI)** 🚧 *in progress*
SQS, SNS, Lambda, StepFunctions, Service Bus, HTTP. Each default-on client extension auto-registers its
check on the **dependency category** (deep `healthcheck` layer, never a probe — §3.2 revision), reusing
the client's own SDK handle, deduped by `(Type, Name)`, with a `healthCheck: false` opt-out. The
Warning-on-permission / `ErrorCode`/`StatusCode` classification (§3.4, §3.9) is done.
- ✅ **SQS** — the reference implementation: the two default (DI-handle) `.UseSqs`/`.UseSqs<T>` overloads
  auto-register `SqsHealthCheck` via `AddDependencyHealthCheck` (dedup `"Sqs:{queueUrl}"`), `healthCheck:
  false` opts out. The `action`-based (hand-wired-client) overloads don't auto-wire.
- ⬜ SNS, Lambda, StepFunctions, Service Bus, HTTP — replicate the SQS pattern.
- ⬜ Default the auto-wired checks under `CachingHealthCheckProcessor` (§3.6) so the deep layer doesn't
  re-hit every dependency per scrape.

**Phase 2 — non-destructive fixes + BYO helper** ✅ *done*
- ✅ Read-only swaps for SQS/Lambda/StepFunctions; carve out the opt-in active tier with a distinct
  `Type` (§3.3). Added `HealthCheckMode` (`Reachability` default / `Active`); reachability probes are
  `GetQueueAttributes` / `GetFunctionConfiguration` / `DescribeStateMachine`; active tier reports
  under `"<Type>.Active"`. Failures report the exception **type**, never the message.
- ✅ `AddHealthCheck(kind, name, Func<Task>)` lambda helper + `Func<Task<bool>>` sibling (§3.8).

**Phase 3 — DX: make it discoverable (without this the capability stays unused, like today)**
- Rework `examples/Aws/Benzene.Examples.Aws/StartUp.cs` to show both halves (auto-covered clients +
  one hand-written lambda dependency); delete the `SimpleHealthCheck` array; verify via
  `Benzene.Examples.sln` (not on the main CI gate).
- Invert `docs/health-checks.md`: lead with one-line-on, clients-already-covered, add-your-own-in-one-line.
- `ValidateHealthChecks()` (warn-by-default, `strict` opt-in) (§3.10).

**Phase 4 — fill coverage gaps (born correct with the standard pattern)**
EventBridge, Event Hub, Event Grid, Queue Storage, gRPC, Kafka, RabbitMQ — co-located, auto-wired,
read-only, readiness-scoped.

**Phase 5 — mesh + interop**
- Mesh dependency-inventory increment (pull-plane render of each snapshot's `HealthCheckDependency[]`
  with live status) (§3.11).
- MEL bridge package (§3.13).
- (Gated/later) resource-identity join key + structural topology graph; edge/resource rollup; Kafka
  probe source.

## 5. Decisions needing sign-off
1. **Default-on vs opt-in** for auto-wiring. PO consensus: default-**on** with `healthCheck: false`
   opt-out — **conditional on** readiness-scoping (§3.2) and Warning-on-permission (§3.4/§3.9)
   shipping in the same release. Without those two, downgrade to opt-in (default-on liveness leakage
   is a restart-storm hazard).
2. **Extend `IHealthCheck` now** (§3.5) — one-way door; needed before a 1.0 API freeze.
3. **Scope of the first increment** — ✅ resolved: the `contracts`-topic increment shipped first (§7).
   Recommend **Phase 0 + Phase 1** next for the transport-client auto-wiring.
4. **Probe-separation axis: topic vs tag** (§4 Phase 0b, §7.2) — the shipped increment chose a
   dedicated **topic**. Recommend keeping separation topic-based to match `contracts`/`liveness`/
   `readiness`, and reserving `Tags` for criticality/caching rather than routing.

## 6. One-way doors to get right (summary)
- The DI-registration contract (`IHealthCheck` into the finder's collection) — every future client
  depends on it.
- Readiness-vs-liveness scoping of auto-wired checks (§3.2) — the biggest hazard.
- Error→status classification (permission→Warning; unreachable-critical→Failed) (§3.4).
- A single global opt-out knob, not per-client bespoke bools.
- The `IHealthCheck` DIM extension shape (§3.5).
- Probe-separation axis — topic vs tag (§4 Phase 0b). Shipped increment set the precedent: **topic**.
- `(Type, Name)` dedup key.
- Mesh: reuse existing statuses; resource-identity join key co-designed with the deferred per-topic
  binding key.

---

## 7. Refinement & shipped increment — contract-drift checks belong on **neither** probe

> This section was authored on the `contracts` topic branch and folds into the investigation above.
> §3.2 locks auto-wired *dependency* checks to **readiness, never liveness**. Consumer-side
> **contract-drift** checks — a generated client's `HealthCheckAsync()` reachability + hash
> comparison (`Benzene.Clients.HealthChecks`) — go one step further: they belong on **neither**
> probe, because they are a sharper case of the §3.2 hazard.

**Why stricter than §3.2.** A contract check calls *another service's* health endpoint, which may
itself aggregate that service's dependencies and clients — so it is **transitive**: in liveness it
restarts healthy consumer pods when a *downstream* is slow (a restart storm one hop removed, §3.2's
failure mode amplified); in readiness it propagates the outage *upstream* (the failing provider's
consumers look unready to their consumers, de-routing an entire dependency chain). And **drift is a
versioning signal, not a serve-traffic signal**: a drifted-but-working provider reports `Warning`,
which never flips `IsHealthy` — a pod one contract revision behind serves traffic fine, so neither
restarting nor de-routing it is a sane response. Contract drift belongs in the mesh / alerting, never
a probe verdict.

**Where they go instead — a dedicated `contracts` diagnostic topic** that monitoring/the mesh scrape
and no Kubernetes probe points at. This is the readiness-scope discipline of §3.2 taken to its
conclusion: not "readiness not liveness," but "off the probes entirely." The one narrow exception is
a *hard synchronous* dependency a service genuinely cannot serve traffic without — a targeted
**reachability-only** check may go in **readiness** (never liveness), but the drift portion is still
excluded.

### 7.1 Shipped (this increment)
Additive only — no existing signature changed, safe under the 1.0 API freeze. Sits ahead of the
Phase 0–1 auto-wiring work above (it needs none of the `HealthCheckFinder` harvesting; checks are
registered explicitly on the `contracts` topic), and is compatible with it: when auto-wiring lands,
the readiness-scope category (§3.2) and this contracts topic are distinct surfaces.

- **`Constants.DefaultContractsTopic = "contracts"`** + **`UseContractsCheck(...)`**
  (`Benzene.HealthChecks`) — three overloads mirroring `UseLivenessCheck`/`UseReadinessCheck`;
  responds only to `contracts`, never the healthcheck/probe topics.
- **`ClientHealthCheck`** (`Benzene.Clients.HealthChecks`) — folds a generated client's aggregated,
  drift-annotated `HealthCheckAsync()` response into one `IHealthCheck` result: reachable + matching
  contract → `Ok`, reachable + drift → `Warning` (degraded-not-fatal, does not flip `IsHealthy`),
  unreachable / throws → `Failed`; attaches a `HealthCheckDependency("Service", name)`. Tracks the
  contract relationship, not the provider's transient internal health.
- **`AddContractCheck<TClient>(serviceName)` / `AddContractCheck(serviceName, client)`** — register
  one per downstream service.
- **Tests** (12): adapter outcomes incl. the drift-doesn't-flip-aggregate-`IsHealthy` guarantee, and
  topic-separation (`UseContractsCheck` answers only `contracts`; readiness/liveness/healthcheck never
  trigger it and it never runs under readiness).
- **Docs**: `docs/kubernetes-health-checks.md` (new subsection), `docs/cookbooks/contract-testing.md`,
  both package `CLAUDE.md`s, `CHANGELOG.md`.
- **Runnable example**: the Mesh example's orders-api exposes a consumer-side contract check against
  payments-api at `GET /contracts` (separate from `/healthcheck`) — verified at runtime to report the
  `payments-api` check as `warning` while `/healthcheck` carries only the DB/cache/queue checks.

### 7.2 Naming note
Earlier drafts of this refinement used the placeholder `dependencies` topic / `UseDependencyCheck` /
`AddClientHealthCheck`. The shipped names are **`contracts`** / `UseContractsCheck` /
`AddContractCheck` — "contracts" reads truer to what the check compares (this is contract testing),
and keeps the topic distinct from the §3.2 dependency-readiness scope.
