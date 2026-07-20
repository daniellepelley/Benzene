# Benzene.Benchmarks

BenchmarkDotNet micro-benchmarks for Benzene's hot paths: the middleware pipeline
(`MiddlewarePipeline<TContext>.HandleAsync`), the request-mapping path
(`MultiSerializerOptionsRequestMapper<TContext>.GetBody<T>`), per-message handler routing
(`MessageHandlerDefinitionLookUp.FindHandler`), and HTTP route matching (`RouteFinder.Find`). This is
the first benchmark suite in the repo — there is no prior baseline to compare against.

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

**These numbers have not yet been generated or verified by anyone.** This suite was authored
without access to a local .NET SDK to actually run it. Treat the first real local run's output as
the baseline to record and compare future changes against — not the numbers in this file, because
there aren't any yet.

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

### `RouteFindingBenchmarks`

- **`Find: hit (first route, extracts a parameter)`** / **`Find: miss (scans every route)`** —
  `RouteFinder.Find` matching an incoming method+path against the registered HTTP routes.

`RouteCount` is parameterized (5/25/100) because a miss scans every route. The cost this targets is
the per-route pattern work: `RouteFinder` now compiles each route's method (lower-cased) and path
pattern (split + `Regex.Split`) once at construction and splits only the incoming path per request,
rather than re-splitting and re-running `Regex.Split` over every route's pattern on every request.
Watch that per-request allocation stays low and doesn't carry the regex/splitting cost that used to
scale with `RouteCount`.

## Why this project is separate from `Benzene.sln`'s test run

`Benzene.Benchmarks` is included in `Benzene.sln` (so CI's `dotnet build Benzene.sln` compile-checks
it), but CI's `dotnet test` step targets `test/Benzene.Core.Test/Benzene.Test.csproj` explicitly, so
this project is never *run* as part of CI. BenchmarkDotNet runs take minutes and produce timing
data, not pass/fail assertions — CI runners are also exactly the noisy/virtualized environment
this README already warns against trusting. Running this suite is a manual, local step.
