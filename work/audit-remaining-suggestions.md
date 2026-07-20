# Remaining audit items — suggestions for maintainer review

Context: the fourth (antipattern-class) audit pass fixed everything that was clean,
behavior-preserving, and low-risk (HTTP header-extraction allocations, MeshAnnouncer dispose race,
MeshSelfReportState torn read, Client.Http disposal, and the per-request route-match memoization).
The items below were **deliberately not auto-fixed** — each needs a design call, a change to the
critical routing/diagnostics path, or knowledge of the host/shutdown model. Concrete recommendations
follow, ordered by value.

---

## 1. `HttpMeshTraceExporter` / `MeshAnnouncer` are never disposed on shutdown (CONFIRMED, real)

**Where:** `Benzene.CloudService/Extensions.cs` (~lines 71–110).

**Finding (verified):** neither the announcer nor the exporter's `DisposeAsync` is ever called by the
container:
- `MeshAnnouncer` is registered `x.AddSingleton(_ => announcer)` (line ~93) "so container disposal
  stops the announce loop," **but nothing ever resolves `MeshAnnouncer` from DI** — it's only used
  via the captured local (`announcer?.EnsureStarted(...)`, `UseAnnouncerStart(..., announcer, ...)`).
  Stock MS DI realizes a singleton **lazily, on first resolve**, and only then adds it to the
  provider's disposal list. A registered-but-never-resolved captured instance is never realized, so
  `ServiceProvider.DisposeAsync()` never disposes it. The announce loop (and its `HttpClient`/CTS)
  leaks until process exit.
- `HttpMeshTraceExporter` is **not registered for disposal at all** — only captured in the
  `envelope.UseMeshTrace(info, traceExporter, ...)` closure. Its `DisposeAsync` is what flushes the
  tail trace batch, so shutdown silently drops the last batch (lossy-by-design mesh, but still).

**Why not auto-fixed:** the effective fix depends on the host's shutdown model (does the DI root get
disposed? is there an `IHostedService`? is `Benzene.HostedService` in play?), and the current
registration reflects a specific intent I shouldn't silently rewire.

**Recommended fix (pick one):**
- **A — make them realize + own their lifetime (cleanest).** Register each as an `IHostedService`
  (or a small `IAsyncDisposable` owner resolved at build) so the host actually starts/stops them.
  The announcer's `EnsureStarted` becomes `StartAsync`; `StopAsync`/`DisposeAsync` awaits the loop
  (already fixed). This also removes the "fire the loop from a captured local" pattern.
- **B — force realization so container disposal works.** After building the provider in the
  CloudService wiring, resolve each once (`resolver.GetService<MeshAnnouncer>()` /
  register + resolve the exporter) so the container tracks them for disposal. Lower-effort but
  relies on the DI root being the one disposed on shutdown.
- Either way: register the exporter for disposal too (it currently isn't), and add a test that
  disposing the built provider disposes both (drives the tail-flush).

**Effort:** small–medium. **Risk:** low, but touches wiring/host semantics → wants your eyes.

---

## 2. `ActivityMiddlewareDecorator` re-resolves the handler per middleware, per request (tracing on)

**Where:** `Benzene.Diagnostics/ActivityMiddlewareDecorator.cs` (~lines 33–53).

**Finding:** when an `Activity`/OTel listener is attached, each middleware span calls
`IMessageGetter.GetTopic(context)` **and** `IMessageHandlerDefinitionLookUp.FindHandler(topic)` to tag
the span — so with N middleware, that's N topic + N handler resolutions per request.

**Already partly addressed:** the topic side now goes through the scoped `MemoizingRouteFinder`
(fixed this pass), so `GetTopic` is one real match + cache hits. The residual is `FindHandler` per
middleware (an indexed dictionary lookup + version select — cheap, but ×N and only when tracing).

**Recommended fix:** resolve `(topic, handler)` **once per request** and tag every span from the
cached pair. Cleanest is a scoped `ResolvedRouteHolder` (same pattern as `PresetTopicHolder`): the
first span computes topic+handler and stores them; subsequent spans read the holder. Alternatively,
extend `MemoizingRouteFinder`'s idea to a scoped `IMessageHandlerDefinitionLookUp` memo. Keep it
strictly no-op when `activity == null` (as today).

**Effort:** small–medium. **Risk:** low (diagnostics-only path), but it's tracing correctness —
verify span tags are unchanged. Given the residual is a cheap lookup gated on tracing, this is
lower-priority than it first looked once #1 landed.

---

## 3. Enrichment dictionary churn per HTTP request

**Where:** `Benzene.Core.MessageHandlers/Request/EnrichingRequestMapper.cs` + the per-transport
enrichers (`AspNetRequestEnricher`, `ApiGatewayRequestEnricher`, `HttpListenerRequestEnricher`).

**Finding:** each HTTP request still allocates several short-lived dictionaries across enrichment —
the accumulator, the query-string `ToDictionary`, the header getter's dict (now cheaper after the
header-path fix), and `CleanUp(route.Parameters)` which does `x.Value.ToString()!.StartsWith("{")`
(string alloc + scan) per route param inside a `Where().ToDictionary()`. Net a handful of throwaway
dictionaries per request; bounded by header/param count.

**Recommended fix:** lower priority now that the header getters and `DictionaryUtils` twin are
optimized. If pursued: have enrichers write into the shared accumulator directly instead of building
intermediate dicts to merge; replace `CleanUp`'s `.ToString().StartsWith("{")` with a check that
doesn't materialize a string (the params are already `object`; test the underlying type/first char).
Best done as a focused enrichment-path pass with an allocation benchmark (add an enrichment scenario
to `Benzene.Benchmarks`) to confirm the win before/after.

**Effort:** medium. **Risk:** medium — enrichment feeds request binding; needs characterization
tests first. Not worth doing blind.

---

## 4. `MeshSelfReportMiddleware` fire-and-forget is unreliable on Lambda (design)

**Where:** `Benzene.Mesh.Reporting/MeshSelfReportMiddleware.cs` (~line 47).

**Finding:** it publishes the self-report with `_ = PublishBestEffortAsync()` **after** `await next()`.
On AWS Lambda — the exact on-demand host this feature targets (per the package's own docs) — the
runtime freezes/kills the execution environment once the handler returns, so a task started after the
response is often never run to completion. The self-report it's meant to piggyback on real traffic is
therefore unreliable on Lambda specifically.

**Why not auto-fixed:** the alternative (await the publish, bounded by a short timeout, before the
handler returns) adds latency to the request path — a genuine tradeoff the current design explicitly
rejects ("never delays the response"). That's a product call.

**Recommended options:**
- **A — bounded await:** `await PublishBestEffortAsync()` wrapped in a short `WhenAny(..., Task.Delay(t))`
  so it completes on Lambda but caps added latency (e.g. 50–100 ms). Best-effort still swallows
  failures.
- **B — host-aware:** fire-and-forget on long-running hosts (where the detached task genuinely runs),
  bounded-await only when on Lambda (detectable via `AWS_LAMBDA_FUNCTION_NAME`).
- **C — accept the gap** and document that self-report on Lambda is opportunistic-and-lossy (the
  package already documents a related staleness gap).

**Effort:** small. **Risk:** low mechanically, but it's a latency/telemetry tradeoff → your call.

---

## Already fixed this audit (for reference)
Header-extraction allocations (AspNet ×2 + `Benzene.Core.Helper.DictionaryUtils` twin +
`DefaultHttpHeaderMappings`); `MeshAnnouncer` CTS use-after-dispose; `MeshSelfReportState` torn read;
`Client.Http` per-call message disposal; per-request route-match memoization (`MemoizingRouteFinder`).
All behavior-preserving, on `main`, full Core suite green.
