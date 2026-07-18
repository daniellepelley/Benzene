# Lambda Cold Start Optimization

Reduce the cold-start latency of a Benzene AWS Lambda — with a mix of Benzene-specific options and
standard .NET Lambda tuning.

## Problem Statement

The first invocation after a scale-up (a cold start) pays for the runtime initializing, your
assembly loading, dependency injection wiring, and Benzene building its pipeline. You want to shrink
that one-off cost so latency-sensitive endpoints stay responsive.

## Prerequisites

- An AWS Lambda Benzene service (see [AWS Lambda Setup](../getting-started-aws.md))
- Familiarity with your SAM/deployment configuration

## What happens on a cold start

`AwsLambdaHost<TStartUp>` builds the DI container and the Benzene middleware pipeline **once**, on
the first invocation, and reuses them for every subsequent invocation on that instance. So the
cold-start work is: runtime init → assembly load → `GetConfiguration` → `ConfigureServices` →
`Configure` (pipeline build). The optimizations below each target one of those.

## Step-by-Step Implementation

### 1. Replace reflection handler discovery with the source generator

By default `AddMessageHandlers(assembly)` discovers handlers by **reflection** at startup. The
`Benzene.CodeGen.SourceGenerators` package generates that registration at **compile time** instead,
removing the assembly scan from the cold-start path.

```bash
dotnet add package Benzene.CodeGen.SourceGenerators --prerelease
```

```csharp
// Instead of scanning at runtime:
services.UsingBenzene(x => x.AddMessageHandlers(typeof(MyHandler).Assembly));

// Use the compile-time generated registration:
services.UsingBenzene(x => x.AddGeneratedMessageHandlers());
```

`AddGeneratedMessageHandlers()` is generated from your handlers' `[Message]` attributes, so there's
no reflection scan at startup.

### 2. Prefer arm64 (Graviton)

arm64 Lambdas generally start faster and cost less. Set it in your SAM template:

```yaml
Globals:
  Function:
    Architectures:
      - arm64
    MemorySize: 1024   # more memory also means more CPU during init
```

Memory size scales CPU, and cold-start work is CPU-bound — raising memory often *reduces*
wall-clock cold-start time (and sometimes total cost).

### 3. Enable ReadyToRun / trimming

Ahead-of-time compilation (ReadyToRun) cuts JIT time during init. In your `.csproj`:

```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

Build for the matching runtime identifier (`linux-arm64` for arm64). Trimming can shrink the
package further; test thoroughly, as reflection-based code can be trimmed away — pairing trimming
with the source generator (step 1) reduces that risk.

### 4. Keep the dependency graph lean

`ConfigureServices` runs on cold start, so:
- Only reference the [packages](../reference/packages.md) you use — each is small and focused, so don't
  pull in transports or integrations you don't need.
- Defer expensive initialization (opening a database/Redis connection) until first use rather than
  eagerly in `ConfigureServices`. Benzene's [Redis cache service](redis-caching.md), for example,
  opens its connection lazily in the background.

### 5. Consider provisioned concurrency for critical paths

For endpoints that can't tolerate any cold start, use AWS **provisioned concurrency** to keep warm
instances ready. It has a cost trade-off, so reserve it for latency-critical functions.

## Testing / measuring

Measure before and after — cold-start optimization is easy to guess wrong:
- Look at the `Init Duration` reported in the Lambda CloudWatch logs (that's the cold-start init).
- Compare across a few deploys, changing one variable at a time (arm64, memory, ReadyToRun,
  generated handlers).

## Troubleshooting

### `AddGeneratedMessageHandlers()` isn't found

**Problem**: The generated method doesn't resolve.

**Solution**: Ensure `Benzene.CodeGen.SourceGenerators` is referenced (as an analyzer) and the
project builds — the method is emitted into the `Benzene.Core.MessageHandlers.DI` namespace at
compile time. Rebuild so the generator runs.

### Raising memory didn't help

**Problem**: More memory didn't reduce cold start.

**Solution**: If init is dominated by I/O (e.g. eagerly connecting to a database), extra CPU won't
help — defer that work to first use instead (step 4).

## Variations

### Native AOT

For the smallest cold starts, .NET Native AOT is an option, but it constrains reflection heavily —
the source-generator registration (step 1) is effectively a prerequisite. Validate the whole
pipeline under AOT before adopting it.

## Further Reading

- [AWS Lambda Setup](../getting-started-aws.md) - the Lambda host and pipeline build
- [Package Reference](../reference/packages.md#code-generation--tooling) - the source-generator package
- [Redis Caching](redis-caching.md) - an example of lazy connection init
- [AWS: Lambda performance](https://docs.aws.amazon.com/lambda/latest/dg/best-practices.html)
