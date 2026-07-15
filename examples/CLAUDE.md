# Benzene Examples — Guide for Claude Code

## What this is
Runnable sample applications that demonstrate Benzene across every host and transport it supports.
Their main job is to show the framework's central promise in practice: **write your message handlers
once, host them anywhere.** The same handlers run behind ASP.NET, AWS Lambda, Azure Functions, gRPC,
Kafka, and Google Cloud by swapping only the host wiring.

These are documentation-by-example and manual/deploy test beds — not the library itself (that's
`src/`) and not the unit tests (that's `test/`).

## The shared domain: `App/Benzene.Examples.App`
The most important project here. It holds the transport-agnostic business logic — `Handlers/`,
`Validators/`, `Services/`, `Model/Messages/`, `Data/`, `Results/` — and is referenced by almost every
host example (Asp, Aws, Azure, Google, Grpc, Kafka). A host example is then mostly just a `StartUp`
that wires this shared domain onto one transport. When adding a demo capability, prefer putting the
handler/logic in `Benzene.Examples.App` and wiring it from the hosts, rather than duplicating it.

`App/Benzene.Examples.App.Data` is a small companion data project.

## Layout (one folder per host/transport)
- **`App/`** — shared handlers/validators/services (above); the reused core.
- **`Asp/`** — ASP.NET Core host. Also where the Spec UI (`/spec-ui`) and the `spec` endpoint are wired.
- **`Aws/`** — AWS Lambda host demonstrating multiple event sources (API Gateway + custom authorizer,
  SNS, SQS, Kafka, EventBridge) in one function.
- **`Azure/`** — Azure Functions host.
- **`Grpc/`** — gRPC host (+ a client project).
- **`Kafka/`** — Kafka consumer and producer.
- **`Google/`** — Google Cloud host (built on `Benzene.AspNet.Core` + `Benzene.Http`).
- **`CodeGen/`** — client code generation from a spec (`Benzene.CodeGen.Client`, `Benzene.Schema.OpenApi`);
  does **not** use the shared `App` domain.
- **`OpenTelemetry/`** — observability demo (`Benzene.OpenTelemetry`, traces/metrics); has its own
  `README.md` and a `wwwroot/` message-sender page. Does **not** use the shared `App` domain.
- **`Mesh/`** — service-mesh visibility demo: three tiny demo services plus an aggregator app that
  dogfoods `Benzene.Mesh.Aggregator`/`Benzene.Mesh.Ui` (self-serves the dashboard via `UseMeshUi`);
  has its own `README.md` and `run.sh`. Does **not** use the shared `App` domain.

## How these build (important)
- Examples build via **`Benzene.Examples.sln`** at the repo root — **not** the main `Benzene.sln`.
  Several folders also have their own solution (`Benzene.Example.Asp.sln`, `Benzene.Examples.Aws.sln`,
  `Benzene.Example.Azure.sln`, `Benzene.Example.Grpc.sln`, `Benzene.Example.Kafka.sln`).
- **The examples are NOT part of the primary CI gate.** `build-benzene.yml` builds `Benzene.sln` and the
  library tests only. The examples are exercised by the deploy workflows
  (`.github/workflows/deploy-asp-example.yml`, `deploy-aws-example.yml`) and otherwise by building
  `Benzene.Examples.sln` locally. So **a change here is not compile-checked by the main build** — if you
  edit an example, build `Benzene.Examples.sln` (or the relevant per-folder `.sln`) to verify it.
- Examples reference `src/` projects directly via `ProjectReference` (they track local source), not the
  published NuGet packages. Adding a new Benzene dependency to an example means adding a `ProjectReference`
  to the `src/` project.

## Conventions
- A new transport/host example: reference `Benzene.Examples.App` for the domain, add a `StartUp` that
  wires it onto the transport, and mirror the structure of an existing sibling (Asp/Aws are the fullest).
- Some example test projects exist (`*.Test` / `*.Tests`, and `*.Dev.Test` for tests needing a running
  dependency like localstack/kafka). Keep new example tests in the same shape.

## Known quirks — do not "tidy" casually
- **Inconsistent naming:** some folders use `Benzene.Example.*` (singular — Asp, Grpc, Azure) and others
  `Benzene.Examples.*` (plural — Aws, Google, Kafka, App, CodeGen, OpenTelemetry). Leave it unless asked;
  renaming a project touches its `.csproj`, every `.sln` that lists it, and every `ProjectReference` to it.
- **`Kakfa` typo:** `examples/Kafka/Benzene.Examples.Kakfa` and `…Kakfa.Producer` are misspelled. Same
  caution — a rename is a multi-file `.sln`/reference change, not a casual fix.

## Startup model
All host examples use the platform-neutral `BenzeneStartUp` (`Configure(IBenzeneApplicationBuilder app, …)`),
wired onto a transport inside `app.UseAwsLambda(…)` / `app.UseWorker(…)` etc. The old host-specific
startup base classes (`AwsLambdaStartUp`, `BenzeneWorkerStartup`, `BenzeneHostedServiceStartup`, and the
example-local `AutofacAwsStartUp`) have been removed.
- **`Aws/`** — `StartUp : BenzeneStartUp` hosted by `Function : AwsLambdaHost<StartUp>` (the Lambda handler
  entry point). Tests build the host with `BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost()`.
- **`Kafka/`** — `StartUp : BenzeneStartUp` run via `Host…UseBenzene<StartUp>()` (registers the worker as an
  `IHostedService`).

## Do NOT
- Do not modify `Benzene.Examples.sln` / the per-folder `.sln` structure without explicit approval.
- Do not add example projects to the main `Benzene.sln` — examples belong to `Benzene.Examples.sln`.
- Do not assume the main CI verifies example changes — it doesn't; build the examples solution yourself.
