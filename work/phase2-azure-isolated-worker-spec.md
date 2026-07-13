# Work spec: Phase 2 — Azure Functions isolated-worker rewrite on BenzeneStartUp

**Status: blocked on dependency approval — do not start implementation until a maintainer approves
the new NuGet package references listed in "Required new dependencies" below.** Per CLAUDE.md, new
NuGet dependencies require explicit approval separately from the "breaking changes are fine"
direction that authorized the rest of this cross-platform unification effort.

This is the last unimplemented adapter from the startup-unification arc (see
`work/cross-platform-design-review.md` §2 and the completed `AwsLambdaHost<TStartUp>` /
`IHostBuilder.UseBenzene<TStartUp>()` / ASP.NET Core `WebApplicationBuilder.UseBenzene<TStartUp>()`
adapters already on `main`). All research is done; this document is a complete, executable work
order once the dependency question is resolved.

## Why this is bigger than the other three adapters

AWS Lambda, the generic host, and ASP.NET Core adapters were each a same-day, additive, no-new-dependency
slice. Azure is different for three concrete reasons, all verified against the code:

1. **Every `src/Benzene.Azure.Function.*` package currently targets the legacy in-process model**
   (`Microsoft.Azure.WebJobs` + `Microsoft.Azure.Functions.Extensions`), not the isolated-worker model
   (`Microsoft.Azure.Functions.Worker`). The design review calls for isolated-worker only (the
   in-process model is EOL). Rewriting onto isolated-worker means swapping package references across
   `Benzene.Azure.Function.Core`, `.AspNet`, `.EventHub`, `.Kafka` — a genuine new-dependency change,
   not a refactor of existing references.
2. **No isolated-worker middleware exists anywhere in this codebase today.** AWS/Worker/ASP.NET all had
   an existing single-scope-per-invocation hosting model to hang `BenzeneStartUp` off of. Azure's
   isolated worker needs a `Microsoft.Azure.Functions.Worker.Middleware.IFunctionsWorkerMiddleware`
   implementation authored from scratch — there's no prior art in this repo to adapt.
3. **Existing test coverage must not silently regress.** Three test files
   (`test/Benzene.Core.Test/Azure/{AspNetPipelineTest,EventHubPipelineTest,KafkaPipelineTest}.cs`) give
   `Benzene.Azure.Function.Core` 82.8%, `.AspNet` 81.0%, `.EventHub` 86.3%, `.Kafka` 90.7% coverage
   (per `work/azure-roadmap-1.0.md`, 2026-07-12 measurement) — all built on `InlineAzureFunctionStartUp`.
   These must be ported forward, not dropped.

## Required new dependencies (get explicit approval before starting)

Per project, in-process → isolated-worker equivalents:

| Project | Remove | Add |
|---|---|---|
| `Benzene.Azure.Function.Core` | `Microsoft.Azure.WebJobs` 3.0.39, `Microsoft.Azure.Functions.Extensions` 1.1.0 | `Microsoft.Azure.Functions.Worker` (latest 2.x), `Microsoft.Azure.Functions.Worker.Sdk` |
| `Benzene.Azure.Function.AspNet` | (none directly; only `FrameworkReference Microsoft.AspNetCore.App`) | `Microsoft.Azure.Functions.Worker.Extensions.Http` (+ `.AspNetCore` if bridging to `HttpRequest` — see decision D3 below) |
| `Benzene.Azure.Function.EventHub` | `Microsoft.Azure.WebJobs.Extensions.EventHubs` 6.3.5 | `Microsoft.Azure.Functions.Worker.Extensions.EventHubs` |
| `Benzene.Azure.Function.Kafka` | `Microsoft.Azure.WebJobs` 3.0.39, `Microsoft.Azure.WebJobs.Extensions.Kafka` 3.9.0 | Isolated-worker Kafka extension support is less mature than HTTP/EventHub — **verify current availability/version before committing to this row**; if no isolated-worker Kafka trigger extension exists, Kafka support may need to stay in-process (documented exception) or be dropped from this rewrite's scope. |

`examples/Azure/Benzene.Example.Azure.csproj` already references the isolated-worker SDK packages
(`Microsoft.Azure.Functions.Worker` 2.52.0, `.Worker.Extensions.Http` 3.3.0, `.Worker.Sdk` 2.0.7) —
use those versions as the starting point, checking for newer releases.

## What already exists to build on (verified, do not re-derive)

- `IBenzeneApplicationBuilder` — `src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneApplicationBuilder.cs`:
  `string Platform { get; }`, `IMiddlewarePipelineBuilder<TContext> Create<TContext>()`.
- `BenzeneStartUp` — `src/Benzene.Microsoft.Dependencies/BenzeneStartUp.cs`: abstract base closing
  `IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>`.
- `BenzeneApplicationBuilder` — `src/Benzene.Core.Middleware/BenzeneApplicationBuilder.cs`: base class
  every platform subclasses (see `AwsLambdaApplicationBuilder`, `WorkerApplicationBuilder` for the
  established pattern — platform ctor takes platform-specific pipeline/state, `Platform` is a `const
  string`, a `UseXxx(this IBenzeneApplicationBuilder, ...)` extension type-checks and no-ops elsewhere).
- `IBenzeneInvocation`/`IBenzeneInvocationAccessor` — `src/Benzene.Abstractions.Pipelines/Hosting/` +
  `src/Benzene.Core.Middleware/BenzeneInvocation*.cs`: the feature-bag pattern already implemented for
  AWS (`ILambdaContext`) and ASP.NET (`HttpContext`) — Azure should add `FunctionContext` the same way
  once the isolated-worker context is available (`.UseBenzeneInvocation()` on whatever Azure's
  per-platform pipeline builder ends up being).
- Existing Azure abstractions to preserve conceptually (their CONCRETE types will need isolated-worker
  equivalents, but the shape/verb names should carry over): `IAzureFunctionAppBuilder`
  (`src/Benzene.Azure.Function.Core/AzureFunctionAppBuilder.cs` — `Add(...)`, `Create<TContext>()`,
  `Create(IServiceResolverFactory)`), the `UseHttp`/`UseEventHub`/`UseKafka` verbs (each package's
  `DependencyInjectionExtensions.cs`), `AzureFunctionApp`/`IAzureFunctionApp` (the per-invocation
  multi-trigger-type dispatcher — this indirection is exactly what worker middleware dispatch should
  replace, per the design review).
- `InlineAzureFunctionStartUp` (`src/Benzene.Azure.Function.Core/InlineAzureFunctionStartUp.cs`) — the
  fluent test/standalone builder all 3 existing Azure test files use. Preserve its call-site shape if
  at all possible so the 3 test files port with minimal changes (only their `Build()`/setup lines should
  need to change, not their assertions).

## Design questions to resolve before writing code

**D1. Where does `AzureFunctionApp`'s multi-trigger dispatch move to?**
Today `AzureFunctionApp` (`src/Benzene.Azure.Function.Core/AzureFunctionApp.cs`) is a scoped,
per-invocation object trigger classes inject and call (`HandleAsync<TRequest,TResponse>`/
`HandleAsync<TRequest>`), doing a linear type-scan over registered sub-applications. The design
review's target replaces this with worker middleware (`ConfigureFunctionsWorkerDefaults(w =>
w.UseBenzene())`) that dispatches directly from `FunctionContext`. Decide: does the new
`IFunctionsWorkerMiddleware` implementation route based on `context.FunctionDefinition.Name`/binding
metadata directly to a matching sub-application (mirroring `AzureFunctionApp`'s existing type-scan
logic, just moved earlier in the pipeline), or does it keep something like `IAzureFunctionApp` as an
internal implementation detail the middleware delegates to? Prefer the latter if it minimizes disruption
to `UseHttp`/`UseEventHub`/`UseKafka`'s existing registration shape.

**D2. Naming collision flagged by prior research — resolve explicitly.**
The design review's own target snippet needs TWO different `UseBenzene()` extensions:
`IHostBuilder.UseBenzene<TStartUp>()` (registers services + builds the pipeline, mirroring
`Benzene.HostedService`'s existing extension exactly) and `IFunctionsWorkerApplicationBuilder.UseBenzene()`
(registers the `IFunctionsWorkerMiddleware`, called inside `ConfigureFunctionsWorkerDefaults(w => ...)`).
These extend different types (`IHostBuilder` vs `IFunctionsWorkerApplicationBuilder`) so C# overload
resolution disambiguates by receiver type — just confirm this compiles cleanly and reads unambiguously
at the call site before committing to the naming; if it's ever ambiguous or confusing, rename the
middleware-registration one (e.g. `UseBenzeneWorkerMiddleware()`).

**D3. HTTP trigger type: bridge to `HttpRequest` or adopt `HttpRequestData` natively?**
The isolated worker's native HTTP trigger type is `Microsoft.Azure.Functions.Worker.Http.HttpRequestData`,
not `Microsoft.AspNetCore.Http.HttpRequest` (which `Benzene.Azure.Function.AspNet`'s existing
`AspNetContext`/`AspNetApplication` wrap). Two paths:
  - (a) Add `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` to bridge triggers to
    `HttpRequest`/`HttpResponse` (ASP.NET Core integration for isolated worker), letting
    `Benzene.Azure.Function.AspNet`'s existing context/adapter classes keep working almost unchanged.
  - (b) Rewrite `Benzene.Azure.Function.AspNet`'s context to wrap `HttpRequestData`/`HttpResponseData`
    natively, no bridge package.
  Path (a) is very likely less work and lower risk (reuses proven adapter code); default to it unless
  there's a concrete reason to avoid the extra package.

**D4. Kafka isolated-worker support.**
Verify `Microsoft.Azure.Functions.Worker.Extensions.Kafka` (or equivalent) actually exists and is
production-ready before committing to rewriting `Benzene.Azure.Function.Kafka`. If it doesn't exist or
is preview-only, flag this back to the maintainer rather than guessing — this may mean Kafka support
stays on the in-process model as a documented exception, decoupled from this rewrite.

## Test porting plan

For each of the 3 existing test files, keep the `[Fact]` method bodies' assertions unchanged; only the
setup (currently `new InlineAzureFunctionStartUp().ConfigureServices(...).Configure(app => app.UseXxx(...)).Build()`)
should change to whatever the isolated-worker equivalent construction looks like. Do not delete or
weaken any assertion — if an isolated-worker equivalent genuinely can't reproduce something the
in-process test verified, stop and flag it rather than silently dropping coverage.

- `test/Benzene.Core.Test/Azure/AspNetPipelineTest.cs` — `Send`, `Send_Xml`, `Send_ValidationError`.
- `test/Benzene.Core.Test/Azure/EventHubPipelineTest.cs` — `Send`, `Send_Xml`.
- `test/Benzene.Core.Test/Azure/KafkaPipelineTest.cs` — `Send`.

Also port `examples/Azure/Benzene.Example.Azure/`: delete the mixed-model `Program.cs`
(`ConfigureFunctionsWorkerDefaults()` + manual `.ConfigureWebJobs((context, builder) => new
StartUp().Configure(builder))`) and the unused `[assembly: FunctionsStartup(typeof(StartUp))]`
attribute, replacing with the new pure isolated-worker `Program.cs` per the design review's target
snippet; `StartUp.cs` becomes a `BenzeneStartUp` subclass; `HttpFunction.cs`'s constructor-injected
`IAzureFunctionApp` indirection should simplify per whatever D1 resolves to (ideally the trigger
function becomes closer to a one-line pass-through, or is eliminated for HTTP if a catch-all proxy
function approach is adopted — the design review floats this but does not mandate it).

## Guardrails

- This is the one adapter in the startup-unification arc where deleting the OLD path
  (`AzureFunctionStartUp`, `IWebJobsStartup` support, the WebJobs package family) is explicitly
  in-scope and expected — the design review calls this out as **(breaking)** and intentional, unlike
  the other three adapters which were purely additive. Don't try to keep both models working side by
  side; that's the "two hosting models mixed" problem this rewrite exists to fix.
- Still preserve: the `UseHttp`/`UseEventHub`/`UseKafka` verb names and their existing configuration
  lambda shapes as closely as possible, so a user migrating an existing Azure StartUp has the smallest
  possible diff.
- Follow existing code style: file-scoped namespaces, XML doc comments on public types.
- Match the naming/base-class pattern established by the other three adapters
  (`AwsLambdaHost<TStartUp>`, `WorkerApplicationBuilder`, `AspApplicationBuilder` +
  `WebApplicationBuilder.UseBenzene<TStartUp>()`) rather than inventing a new shape for Azure alone.

## Verification & delivery

1. `dotnet build Benzene.sln` — 0 errors.
2. `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` — full green, no regression from the
   current baseline (653 passed / 4 skipped as of this spec's authoring — check `git log` on this
   branch for the current number before starting).
3. `dotnet build Benzene.Examples.sln` — 0 errors, including the rewritten `examples/Azure` project.
4. If real Azure Functions tooling is available in the environment (Core Tools / `func start`), smoke-test
   the rewritten example locally; if not available, say so explicitly rather than claiming it works.
5. Commit to `claude/benzene-cross-platform-design-ra60co`, push with `git push -u origin
   claude/benzene-cross-platform-design-ra60co`; then, if `git merge-base --is-ancestor origin/main HEAD`
   succeeds after `git fetch origin main`, also `git push origin HEAD:main`.
6. In the commit message, explicitly list every new NuGet package added (matching what was approved)
   and flag any design-question deviation (D1–D4) from what's written here.
