# Benzene.Benchmarks

BenchmarkDotNet micro-benchmarks for Benzene's hot paths: the middleware pipeline
(`MiddlewarePipeline<TContext>.HandleAsync`) and the request-mapping path
(`MultiSerializerOptionsRequestMapper<TContext>.GetBody<T>`). This is the first benchmark suite in
the repo — there is no prior baseline to compare against.

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

## Why this project is separate from `Benzene.sln`'s test run

`Benzene.Benchmarks` is included in `Benzene.sln` (so CI's `dotnet build Benzene.sln` compile-checks
it), but CI's `dotnet test` step targets `test/Benzene.Core.Test/Benzene.Test.csproj` explicitly, so
this project is never *run* as part of CI. BenchmarkDotNet runs take minutes and produce timing
data, not pass/fail assertions — CI runners are also exactly the noisy/virtualized environment
this README already warns against trusting. Running this suite is a manual, local step.
