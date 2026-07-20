# Benzene.Benchmarks

BenchmarkDotNet micro-benchmarks for Benzene's hot paths: the middleware pipeline
(`MiddlewarePipeline<TContext>.HandleAsync`), the request-mapping path
(`MultiSerializerOptionsRequestMapper<TContext>.GetBody<T>`), per-message handler routing
(`MessageHandlerDefinitionLookUp.FindHandler`), per-message handler-pipeline construction
(`HandlerPipelineBuilder.Create`), and HTTP route matching (`RouteFinder.Find`).

## Running

```bash
dotnet run -c Release --project benchmarks/Benzene.Benchmarks -- --filter '*'
```

Omit `--filter '*'` for BenchmarkDotNet's interactive picker if you only want to run one class.

**Must use `-c Release`.** A Debug build disables the JIT optimizations these numbers depend on;
BenchmarkDotNet will refuse to run (or warn loudly) against a Debug build.

**Don't trust numbers from constrained, shared, or virtualized environments** — CPU-throttled
containers, noisy-neighbor CI runners, or a sandboxed environment with no dedicated hardware all
produce unreliable absolute numbers. Relative comparisons within the same run on the same machine
are far more trustworthy than any single absolute ns/op or bytes-allocated figure quoted in
isolation.

**Most of these benchmarks have not yet been run on trusted hardware.** The suite was authored
without access to a dedicated machine. The one exception is `HandlerCreationBenchmarks`, whose
*allocation* figures are recorded under "Recorded baselines" below (allocations are deterministic
regardless of the environment; the timings from that run are not). Treat each other benchmark's
first real local run as its baseline to record and compare future changes against — not any number
absent from the baselines section.

## What each benchmark measures

### `MiddlewarePipelineBenchmarks`

- **`HandleAsync only (chain construction, shared resolver)`** — isolates
  `MiddlewarePipeline<TContext>.HandleAsync`'s own cost (building the middleware chain and invoking
  it) against a long-lived `IServiceResolver` reused across every call.
- **`HandleAsync with a fresh scope per call (realistic per-request cost)`** — the same call, but
  also creates and disposes a fresh DI scope per call, matching how every real transport adapter
  (ASP.NET Core, Lambda, SQS batch records, ...) actually invokes `CreateScope()` once per
  request/message.

These are deliberately reported separately, not as one combined number, so DI-scope-creation cost
and middleware-chain-construction cost aren't conflated. `MiddlewareCount` is parameterized
(1/5/20) because the audited cost — each middleware's `IMiddlewareFactory.Create` call, and
previously an `Enumerable.Reverse()` over the whole array on every single call — scales with chain
length.

### `RequestMappingBenchmarks`

- **`GetBody: first call (negotiate + build mapper cache)`** — a fresh
  `MultiSerializerOptionsRequestMapper<TContext>`'s first `GetBody<T>()` call: media-format
  negotiation plus building and caching its serializer-specific mapper pair.
- **`GetBody: warmed cache (steady-state deserialization)`** — an unmeasured priming call followed
  by the measured call, showing steady-state deserialization cost once that cache is warm.

A fresh mapper is constructed inside each `[Benchmark]` method rather than reused from
`[GlobalSetup]`, because that's genuinely how it's used in production (a scoped, per-message
instance) — reusing one across iterations would misrepresent the real allocation pattern.

### `HandlerRoutingBenchmarks`

- **`FindHandler: requested version is the first registered`** / **`... last registered`** —
  `MessageHandlerDefinitionLookUp.FindHandler` resolving a topic (id + version) to a handler
  definition against the shared, pre-built `MessageHandlerDefinitionIndex` (the index build is warmed
  once in `[GlobalSetup]`, so the measured calls are steady-state lookups).

`VersionsPerTopic` is parameterized (1/5/20) because that's the dimension version-selection cost
scales with: the lookup picks one of N registered versions for the topic id. This selection used to
be folded into the per-candidate `FirstOrDefault` predicate, re-running the whole selection and
re-allocating its candidate-version array once per candidate — O(n²) work and allocation per
dispatch — before being hoisted to run once. The suite makes that cost visible and guards it from
regressing (watch that allocated bytes stay flat, not scaling with `VersionsPerTopic`).

### `HandlerCreationBenchmarks`

- **`Build the handler pipeline (the per-message rebuild)`** — `HandlerPipelineBuilder.Create`, the
  work `MessageHandlerFactory.Create` → `PipelineMessageHandlerWrapper.Wrap` does on *every*
  dispatched message: a fresh `List`, one middleware instance per registered
  `IHandlerMiddlewareBuilder`, a `.Select(...).ToArray()` into a `Func[]`, and a new
  `MiddlewarePipeline` whose constructor reverses the array again.
- **`Build + invoke: rebuild the pipeline then run one message through it`** — the same build,
  wrapped in a `PipelineMessageHandler` and run once, to show what fraction of total per-message
  dispatch allocation is the (removable) structure rebuild vs. the (inherent) per-invocation cost.

`HandlerMiddlewareCount` is parameterized (0/1/3) because the structure-build cost *used to* scale
with it. The pipeline *structure* (which builders, in what order) is fixed after startup — only the
middleware *instances* are genuinely per-scope — so the top-level `MiddlewarePipeline`'s
"structure once, instances per request" split applies here too. `HandlerPipelineBuilder.Create` now
resolves that structure from a per-(builder-set, request, response) cache
(`HandlerPipelineStructureCache` / `HandlerMiddlewarePipeline`) and wraps it with the current
message's handler, instead of rebuilding the `List` + middleware instances + `Func[]` +
`MiddlewarePipeline` per message. This suite is the regression guard for that fix (see the
before/after under "Recorded baselines"). A long-lived resolver is reused so it isolates build/invoke
cost from DI-scope-creation cost (which `MiddlewarePipelineBenchmarks` covers separately).

### `RouteFindingBenchmarks`

- **`Find: hit (first route, extracts a parameter)`** / **`Find: miss (scans every route)`** —
  `RouteFinder.Find` matching an incoming method+path against the registered HTTP routes.

`RouteCount` is parameterized (5/25/100) because a miss scans every route. The cost this targets is
the per-route pattern work: `RouteFinder` now compiles each route's method (lower-cased) and path
pattern (split + `Regex.Split`) once at construction and splits only the incoming path per request,
rather than re-splitting and re-running `Regex.Split` over every route's pattern on every request.
Watch that per-request allocation stays low and doesn't carry the regex/splitting cost that used to
scale with `RouteCount`.

## Recorded baselines

Absolute timings from a constrained/virtualized environment are not trustworthy (see the warning
above — note the wide `Error` bars). **Allocated bytes/op, however, are deterministic** and don't
depend on CPU timing, so they are the figures to hold onto and diff against.

### `HandlerCreationBenchmarks` — before/after the structure-caching fix (ShortRun, allocations)

The per-message handler-pipeline rebuild used to be a **~408 B fixed floor plus ~128 B per
handler-middleware** — all of it structure that is fixed after startup — which was **~30–40% of the
total per-message allocation** on the dispatch path. Caching the structure removed it:

| Benchmark | HandlerMiddlewareCount | Allocated (before) | Allocated (after) |
|-----------|-----------------------:|-------------------:|------------------:|
| Build the handler pipeline | 0 | 408 B | **32 B** |
| Build the handler pipeline | 1 | 536 B | **32 B** |
| Build the handler pipeline | 3 | 792 B | **32 B** |
| Build + invoke (full per-message dispatch) | 0 | 1312 B | **968 B** |
| Build + invoke (full per-message dispatch) | 1 | 1568 B | **1120 B** |
| Build + invoke (full per-message dispatch) | 3 | 2080 B | **1424 B** |

Reading: the "Build" row is now a flat, non-scaling **32 B** — the single per-message
`HandlerMiddlewarePipeline` wrapper carrying that message's handler, the one allocation that can't be
cached away — no longer growing with middleware count. "Build + invoke" fell by the removed build
cost; its residual growth is the invoke-time chain closures (which the old code also allocated at
invoke). This is the baseline to diff future changes against: the "Build" row must stay flat and
small, and `HandlerPipelineBuilderCachingTest` guards the correctness invariants (cache-key
isolation + per-record scope resolution under concurrency). Timings under this run were dominated by
environment noise (build ~16–31 ns, build+invoke ~2.9–3.3 µs) and are order-of-magnitude context
only, not a baseline.

## Why this project is separate from `Benzene.sln`'s test run

`Benzene.Benchmarks` is included in `Benzene.sln` (so CI's `dotnet build Benzene.sln` compile-checks
it), but CI's `dotnet test` step targets `test/Benzene.Core.Test/Benzene.Test.csproj` explicitly, so
this project is never *run* as part of CI. BenchmarkDotNet runs take minutes and produce timing
data, not pass/fail assertions — CI runners are also exactly the noisy/virtualized environment
this README already warns against trusting. Running this suite is a manual, local step.
