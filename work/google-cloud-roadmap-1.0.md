# Benzene Google Cloud Packages — Roadmap to a Real Integration

**Document Version:** 1.1
**Last Updated:** 2026-07-17
**Owner:** unassigned
**Status:** Phase 1 done

> **2026-07-17 implementation update:** Phase 1 (the Pub/Sub push adapter) is done, per §5's
> phasing table and the priority recommendation in §7. What was built, vs. §4's proposed layout:
> - **`Benzene.GoogleCloud.Functions.PubSub`** - `GooglePubSubFunctionHost<TStartUp> :
>   ICloudEventFunction<MessagePublishedData>` (mirrors `GoogleCloudFunctionHost<TStartUp>`'s exact
>   bootstrap shape for the CloudEvent trigger type instead of HTTP), `PubSubContext`/getters/setter
>   wired through `UseMessageHandlers()`, and `PubSubOptions` (`CatchExceptions`/
>   `RaiseOnFailureStatus`) reusing the same containment/escalation vocabulary
>   `work/batch-failure-handling.md` established for Kafka/ServiceBus. Cloud Functions delivers
>   exactly one Pub/Sub message per invocation (not a batch), so there's no fan-out loop here at
>   all - structurally closer to a single HTTP request than to the batch-oriented AWS/Azure
>   triggers.
> - **`Benzene.GoogleCloud.Functions.PubSub.TestHelpers`** - `BuildGooglePubSubFunctionHost<TStartUp>()` +
>   `SendPubSubAsync(...)`, mirroring the HTTP package's TestHelpers shape, plus
>   `MessageBuilderExtensions.AsPubSubEvent<T>()` bridging the same shared `IMessageBuilder<T>` test
>   abstraction every other transport's test helpers already extend.
> - **Topic convention decided**: the `"topic"` message attribute, matching
>   `Benzene.Aws.Sqs`/`Benzene.Aws.Lambda.Sqs`/`Benzene.Aws.Lambda.Sns`/
>   `Benzene.Azure.Function.ServiceBus`'s existing "topic in a custom attribute/property"
>   convention exactly - no new convention invented.
> - **Not built in this phase** (unchanged from §5's phasing): preset-topic override
>   (`UsePresetTopic`-equivalent wiring - a small, additive follow-up, not done here to keep scope
>   tight), and everything in Phases 2-5 (pull-subscription worker + outbound publish client,
>   `IBenzeneInvocation`/W3C trace-context wiring, Cloud Tasks/Firestore/GCS secondary adapters,
>   `examples/Google` Pub/Sub wiring + `docs/getting-started-google-cloud.md`).
> - **Not verified**: an actual live Pub/Sub subscription delivering a real CloudEvent to a deployed
>   function - this sandbox has no live GCP project or credentials, same caveat as Phase 0. What
>   *is* verified: the full test suite dispatching a real `MessagePublishedData` through
>   `GooglePubSubFunctionApplicationBuilder`'s built pipeline via `Benzene.Testing`'s in-memory test
>   host, plus direct unit coverage of the getters and `PubSubOptions`' failure-handling
>   combinations. The exact shape of `Google.Events.Protobuf.Cloud.PubSub.V1.PubsubMessage`/
>   `MessagePublishedData` (property names, settability) was reconstructed from this repo's own git
>   history (the Phase-0-era `PubSubFunction.cs` stub, since deleted) rather than verified against a
>   live-restored NuGet package, since this sandbox has no network access to nuget.org - flagged as
>   the one thing most worth double-checking against a real `dotnet build` before deploying.

> **2026-07-15 implementation update:** Phase 0 is done. Three of §6's open questions were decided
> and implemented, not left open:
> - **Package naming (open question 1): `Benzene.GoogleCloud.*`**, not `Benzene.Google.*` as this
>   document originally used throughout §4's table and prose — "Google" alone read as ambiguous
>   outside a cloud-provider context, same reasoning the question itself raised. Read `Benzene.Google.*`
>   in §4/§6 below as superseded by this name.
> - **Cloud Run is the primary documented deploy target (open question 2)**, Cloud Functions Gen2
>   the secondary/alternative — exactly the recommendation §6 made.
> - **`examples/Google` was replaced outright (open question 5)**, not kept alongside the new
>   packages — the prior effort to keep it merely compiling (`work/1.0.0-release-status.md`) is
>   superseded, not preserved; there was no reason found to keep the old hand-rolled pipeline once
>   real packages existed.
>
> What was actually built, vs. §4's proposed layout:
> - **`Benzene.GoogleCloud.Functions.Core`** — `GoogleCloudStartUpRunner.Bootstrap<TStartUp>()`,
>   mirroring `Benzene.Aws.Lambda.Core`'s role as shared bootstrap plumbing. No Google-specific NuGet
>   dependency, exactly as §4 proposed.
> - **`Benzene.GoogleCloud.Functions.Http`** — `GoogleCloudFunctionHost<TStartUp> : IHttpFunction` +
>   `GoogleCloudFunctionApplicationBuilder : BenzeneApplicationBuilder, IAspApplicationBuilder`. The
>   design goes one step further than §4 anticipated: because `Benzene.AspNet.Core`'s `IAspApplicationBuilder`
>   has no inherent dependency on a live ASP.NET Core `IApplicationBuilder` (only its existing
>   `AspApplicationBuilder` implementation does), implementing that interface here — instead of
>   hand-rolling a parallel HTTP pipeline the way the old example did — means **the exact same
>   `Startup : BenzeneStartUp` class runs unchanged on both Cloud Run and Cloud Functions Gen2**. See
>   the package's own `CLAUDE.md` for the full mechanism.
> - **`Benzene.GoogleCloud.Functions.Http.TestHelpers`** — `BuildGoogleCloudFunctionHost<TStartUp>()` +
>   `SendHttpAsync(IHttpFunction, HttpContext)`, mirroring the AWS/Azure `.TestHelpers` shape exactly,
>   plus a promoted/generalized `HttpContextBuilder` (System.Text.Json-based, not the old example's
>   Newtonsoft.Json one, to avoid a new NuGet dependency).
> - **Cloud Run needed no new package**, exactly as §4/§6 said — `examples/Google/Benzene.Examples.Google/Program.cs`
>   uses `Benzene.AspNet.Core`'s existing `WebApplicationBuilder.UseBenzene<Startup>()` directly,
>   binding Kestrel to the `PORT` env var Cloud Run injects.
> - `examples/Google` was rewritten around one shared `Startup.cs`, with `Program.cs` (Cloud Run) and
>   `Function.cs` (Cloud Functions Gen2, `class Function : GoogleCloudFunctionHost<Startup> { }` —
>   the same one-line deploy-entry-point convention `AwsLambdaHost<TStartUp>` uses) as the two thin
>   host-specific files, plus a real `Dockerfile` (the roadmap's §1 flagged this as entirely
>   missing). `Benzene.Examples.Google.Tests`' 10 tests were rewired onto `BuildGoogleCloudFunctionHost<Startup>()`
>   and pass, dispatching real `HttpContext`s through the full pipeline — genuine end-to-end
>   verification of the new package, not just a compile check.
> - **Not built in this phase** (unchanged from §5's phasing): Phase 1's `Benzene.GoogleCloud.Functions.PubSub`
>   push adapter (the old `PubSubFunction.cs` stub is simply gone, not replaced — Pub/Sub remains
>   0% per §1's original assessment), and everything in Phases 2-5.
> - **Not verified**: an actual live deployment to Cloud Run or Cloud Functions Gen2 — this sandbox
>   has no live GCP project or credentials. What *is* verified: the full test suite dispatching real
>   requests through `GoogleCloudFunctionApplicationBuilder`'s built pipeline, and a direct in-process
>   `HandleAsync` round-trip through `Function` itself (not just the test-helper reconstruction) — see
>   `examples/Google/README.md`'s Notes section for what that leaves open.

## Purpose

Benzene has mature, production-shaped native adapters for AWS (`Benzene.Aws.*`, 9 production
packages across Lambda event sources + outbound clients) and Azure (`Benzene.Azure.*`, isolated-worker
Functions + AspNet/EventHub/Kafka/ServiceBus). Google Cloud has **none** — despite `docs/Overview.md`
listing "Google Cloud → Function" as a supported host, and `work/cross-platform-design-review.md`
already flagging this gap explicitly ("gRPC / Google Cloud | none | manual | Everything hand-wired in
`Program.cs` / the function class"). This document plans closing that gap to the same standard as AWS
and Azure: a real `src/Benzene.Google.*` package family, `BenzeneStartUp`-based hosting, matching
`TestHelpers` packages, and a getting-started guide — not just an example that happens to compile.

## 1. Current state (verified against actual code, not assumed)

There is **no `src/Benzene.Google.*` package** — `find src -maxdepth 1 -iname "*Google*"` returns
nothing. Everything Google-Cloud-shaped lives in `examples/Google/Benzene.Examples.Google/`, four
files:

- **`HttpFunction.cs`** implements `Google.Cloud.Functions.Framework`'s `IHttpFunction` and — this
  is worth stating clearly, since it's easy to assume the whole example is a throwaway stub — **it
  genuinely works**: it builds a real `MiddlewarePipelineBuilder<AspNetContext>` (`.UseTimer(...)
  .UseMessageHandlers(...)`), wraps it in a real `AspNetApplication`, and forwards the Functions
  Framework's `HttpContext` straight into `_app.SendAsync(context)`. This isn't faked - Cloud
  Functions Gen2's `.NET` Functions Framework literally hosts your function inside a real ASP.NET
  Core Kestrel server and calls `IHttpFunction.HandleAsync(HttpContext)`, so reusing
  `Benzene.AspNet.Core` wholesale here is architecturally correct, not a shortcut. Its gap is
  **staleness and packaging**, not correctness: it predates the `BenzeneStartUp`/
  `IBenzeneApplicationBuilder` unification AWS and Azure were both migrated onto (it hand-rolls
  `MiddlewarePipelineBuilder<AspNetContext>` + `MicrosoftServiceResolverFactory` directly in a
  constructor, and `DependenciesBuilder.cs` hand-rolls DI registration rather than implementing
  `BenzeneStartUp.ConfigureServices`/`Configure`), and it lives in `examples/` with no `src/` package,
  no dedicated tests beyond one in-process ASP.NET Core test host, and no `TestHelpers` package.
- **`PubSubFunction.cs`** implements `ICloudEventFunction<MessagePublishedData>` and is a genuine
  stub: 12 lines that log `"Hello {name}"` from the Pub/Sub message text. It does **not** route
  through `UseMessageHandlers()`, does not use any topic convention, and pulls in no
  `Google.Cloud.PubSub.V1` SDK at all (only `Google.Events.Protobuf`'s CloudEvent data type). This is
  the real gap — async messaging on GCP is unimplemented, not just unpolished.
- No Dockerfile, `cloudbuild.yaml`, or deploy script anywhere under `examples/Google/`; no
  `.github/workflows/*google*` CI/deploy workflow (compare to `deploy-aws-example.yml`); no
  `docs/getting-started-google-cloud.md` (compare to `docs/getting-started-aws.md`/
  `docs/azure-functions.md` - both full onboarding walkthroughs); `docs/Overview.md` has one line
  and no code sample.
- `work/1.0.0-release-status.md` records that this example had **rotted** (missing project
  references after unrelated renames) and was only recently repaired to compile and pass its 10
  existing tests — i.e., recent effort here has been keeping the lights on, not building real
  integration.

**Bottom line:** the HTTP story is ~70% of the way to a real package already; Pub/Sub is 0%; nothing
else (Cloud Tasks, Firestore, Cloud Storage, outbound publish clients, CI/deploy, docs) exists at all.

## 2. Reference architecture — what AWS and Azure already establish as "the pattern"

Every existing transport adapter, regardless of provider, plugs into exactly two universal hook
points (`src/Benzene.Core.MessageHandlers/DI/Extensions.cs` /
`src/Benzene.Core.MessageHandlers/Extensions.cs`): `IBenzeneServiceContainer.AddMessageHandlers(...)`
(DI-side discovery) and `IMiddlewarePipelineBuilder<TContext>.UseMessageHandlers<TContext>(...)`
(pipeline-side dispatch). A Google adapter needs nothing new here - it's transport-agnostic by
design.

**AWS's pattern** (`Benzene.Aws.Lambda.Core`): one shared context type (`AwsEventStreamContext`)
wrapping the raw invocation stream; an `AwsLambdaMiddlewareRouter<TRequest>` base class every
event-source package subclasses to "sniff and claim" its event shape (e.g. SQS checks
`Records[0].EventSource == "aws:sqs"`) and fall through to the next middleware otherwise; a single
host entry point (`AwsLambdaHost<TStartUp> : IAwsLambdaEntryPoint`, generic over `BenzeneStartUp`)
constructed once at cold start. HTTP-shaped Lambda (API Gateway) implements `IHttpContext`/
`IHttpRequestAdapter`/`IBenzeneResponseAdapter` to map a raw JSON proxy event onto Benzene's
transport-neutral HTTP types. Queue consumers (SQS) fan each record out via `Task.WhenAll` into its
own DI scope, reporting partial-batch failures back to the platform for per-record retry.

**Azure's pattern** (`Benzene.Azure.Function.Core`, more recently rebuilt onto the isolated-worker
model - the more modern of the two references): `IHostBuilder.UseBenzene<TStartUp>()` runs
`StartUp.GetConfiguration()`/`ConfigureServices()`/`Configure()` once inside `ConfigureServices`,
builds an `AzureFunctionAppBuilder`, and registers a scoped `IAzureFunctionApp`.
`AzureFunctionApp.HandleAsync<TRequest[,TResponse]>` dispatches by **matching**
`IEntryPointMiddlewareApplication` type against each trigger's request/response types - a
generic-host-plus-typed-triggers shape, not a single request pipeline. This is structurally the
closer analog for Google Cloud Functions Gen2/Cloud Run than AWS's raw-stream model, since GCP's
compute is itself a generic ASP.NET-Core-ish host with typed triggers (`IHttpFunction`,
`ICloudEventFunction<T>`), not a bare JSON-in/JSON-out invocation.

**Cross-cutting packages are free once `IHttpContext`/`IMiddlewarePipelineBuilder<TContext>` exist**
for a new transport: `Benzene.HealthChecks*`, `Benzene.OpenTelemetry`, `Benzene.Schema.OpenApi`, and
`Benzene.Mesh.*` all have zero AWS/Azure-specific branches. Two things do need small, one-time
per-transport work, and Google will need the same: (1) `IBenzeneInvocation` population from the
platform's own invocation context (Azure's `FunctionsWorkerApplicationBuilderExtensions.UseBenzene()`
does this from `FunctionContext.InvocationId` — Google's equivalent is Cloud Functions Framework's
`ILogger`/`HttpContext` request-scoped state), and (2) W3C trace-context extraction for non-HTTP
transports (already an open gap for AWS/Azure's non-HTTP transports too — not Google-specific, and
not blocking).

**Testing**: `Benzene.Testing`'s `BenzeneTestHost.Create<TStartUp>().Build<THost>(factory)` is a
generic bridge; AWS and Azure each add one `Build*TestHelpers` extension on top
(`BuildAwsLambdaHost<TStartUp>()`, `BuildAzureFunctionApp<TStartUp>()`). Every per-event-source
package then adds a `SendXxxAsync(...)` + an `IMessageBuilder<T>.AsXxx()` builder converting
Benzene's transport-agnostic message shape into a realistic native event object. Google needs the
identical shape: `BuildGoogleCloudFunctionHost<TStartUp>()` plus per-adapter `SendXxxAsync`/`AsXxx()`.

## 3. What Google Cloud's compute/messaging surfaces map to

| Benzene concept | AWS | Azure | **Google Cloud (proposed)** |
|---|---|---|---|
| Compute host | Lambda (raw stream) | Functions (isolated-worker, gRPC protocol) | Cloud Functions Gen2 / Cloud Run (both literally ASP.NET Core under Kestrel) |
| HTTP trigger | API Gateway | HTTP trigger | Cloud Functions Framework `IHttpFunction`, or Cloud Run direct (no framework needed at all) |
| Async messaging | SQS (queue) + SNS (fan-out) | Service Bus (queue/topic) | **Pub/Sub** (topic + push/pull subscriptions - covers both queue and fan-out patterns in one service) |
| Streaming | Kinesis, Kafka (MSK) | Event Hubs, Kafka (via Event Hubs) | Pub/Sub (no separate streaming product - GCP unifies this into Pub/Sub with ordering keys) |
| CDC / change streams | DynamoDB Streams | (none yet) | Firestore/Datastore triggers (via Eventarc → Pub/Sub or Cloud Functions triggers directly) |
| Object storage events | S3 (fire-and-forget) | (none yet) | Cloud Storage (delivered via Pub/Sub or Eventarc, not a separate transport) |
| Delayed/scheduled invocation | Step Functions (heavier), EventBridge Scheduler | Durable Functions (not adopted) | **Cloud Tasks** (HTTP-target queue with delay/retry - closest analog) |
| Outbound client | `Benzene.Clients.Aws` | (implicit via SDK) | `Benzene.Clients.Google` (Pub/Sub publish, Cloud Tasks enqueue) |
| Health-check dependency | `SqsHealthCheck`, EF, HTTP | EF, HTTP | Firestore ping (mirrors `Benzene.HealthChecks.EntityFramework`'s pattern) |
| Mesh artifact storage | (S3 - not built) | (Blob - not built) | GCS-backed `IMeshArtifactStore` (same still-open gap AWS/Azure blob storage has - natural three-way follow-up, not Google-specific) |

**Key insight worth stating plainly**: because Cloud Functions Gen2 and Cloud Run are both real
ASP.NET Core hosts under the hood, Google's HTTP story requires almost no new adapter code - the
existing `Benzene.AspNet.Core` package already does the real work. The genuinely new engineering is
entirely on the **Pub/Sub** side, both push (CloudEvent HTTP delivery) and pull
(`Google.Cloud.PubSub.V1.SubscriberClient`, for non-Functions-hosted Benzene apps on GKE/Compute
Engine/a Cloud Run service with `min-instances > 0`).

## 4. Proposed package layout

Naming follows the existing `examples/Google` precedent (`Benzene.Google.*`, not
`Benzene.GoogleCloud.*` - flagged as a real, one-way-door naming decision in §6).

| Package | Responsibility | Depends on |
|---|---|---|
| `Benzene.Google.Functions.Core` | `BenzeneStartUp`-based host wrapper for Cloud Functions Framework - the foundation every other Google package builds on, mirroring `AwsLambdaHost<TStartUp>`/Azure's `IHostBuilder.UseBenzene<TStartUp>()` | `Benzene.Abstractions`, `Google.Cloud.Functions.Framework` |
| `Benzene.Google.Functions.Http` | Promotes the already-working `HttpFunction` pattern into a real, tested package: `IHttpFunction` implementation wired through `BenzeneStartUp`'s real pipeline, for Cloud Functions Gen2 HTTP triggers. Cloud Run direct-Kestrel hosting needs **no package at all** - just `Benzene.AspNet.Core` and a getting-started doc section, since Cloud Run runs a plain ASP.NET Core container | `Benzene.Google.Functions.Core`, `Benzene.AspNet.Core` |
| `Benzene.Google.Functions.PubSub` | Real Pub/Sub **push** adapter: decodes the CloudEvent, extracts topic from a Pub/Sub message attribute (matching the existing SQS/SNS/ServiceBus "topic in a custom attribute" convention), routes through `UseMessageHandlers()`. Replaces today's pipeline-bypassing stub | `Benzene.Google.Functions.Core`, `Google.Events.Protobuf` |
| `Benzene.Google.PubSub` | Pull-subscription background-worker adapter (`SubscriberClient.StartAsync`, for GKE/Compute Engine/always-on Cloud Run) + `Benzene.Clients.Google`-equivalent outbound publish client (`PublisherClient` wrapper) | `Benzene.Mesh.Contracts`-style port pattern not needed here; depends on `Benzene.Core.MessageHandlers`, `Google.Cloud.PubSub.V1` |
| `Benzene.Google.Functions.Tasks` | Cloud Tasks adapter - enqueue via an outbound client, HTTP-target task delivery reuses `Benzene.Google.Functions.Http`'s handling on the receiving end | `Benzene.Google.Functions.Http`, `Google.Cloud.Tasks.V2` |
| `Benzene.HealthChecks.Firestore` *(optional, P2)* | `FirestorePingHealthCheck`, mirroring `Benzene.HealthChecks.EntityFramework`'s pattern | `Benzene.HealthChecks.Core`, `Google.Cloud.Firestore` |
| Matching `.TestHelpers` per adapter above | `SendXxxAsync`/`AsXxx()` builders, mirroring every existing AWS/Azure `.TestHelpers` package exactly | the package it tests + `Benzene.Testing` |

## 5. Phasing

Greenfield integration, so a build-order phase table communicates the plan more clearly than the
prose Q1/Q2 roadmap style used in the (already-mature) AWS/Azure documents:

| Phase | Delivers | Depends on | Risk |
|---|---|---|---|
| 0 | `Benzene.Google.Functions.Core` + `Benzene.Google.Functions.Http`: real package, `BenzeneStartUp`-wired, tests, `.TestHelpers`, replacing the example's hand-rolled pipeline construction. Document Cloud Run as the simpler no-package-needed alternative | nothing new - promotes existing working code | **Low** - closest to done already |
| 1 | `Benzene.Google.Functions.PubSub` (push adapter), replacing the stub, wired to `UseMessageHandlers()` with a documented topic-attribute convention | Phase 0 | Medium - first genuinely new adapter code |
| 2 | `Benzene.Google.PubSub`: outbound publish client + pull-subscription background worker | Phase 1 (shares message-shape conventions) | Medium |
| 3 | `IBenzeneInvocation` wiring + W3C trace-context propagation for the Pub/Sub path, diagnostics parity check against AWS/Azure | Phase 1-2 | Low - same small pattern Azure already established |
| 4 | Secondary adapters: Cloud Tasks, Firestore health check, GCS-backed `IMeshArtifactStore` (three-way cross-sell with the still-open AWS/Azure blob-storage gap), GCS-object-notification-via-Pub/Sub convenience | Phase 1-2 | Low, each independent |
| 5 | `.github/workflows/deploy-google-example.yml`, `docs/getting-started-google-cloud.md` (mirroring the AWS/Azure onboarding docs' shape), and either a rebuilt `examples/Google` or a clear decision to keep/replace the current one (see §6) demonstrating HTTP + Pub/Sub end-to-end with a real Dockerfile/`cloudbuild.yaml` | 0-4 | Low - packaging/docs work |

Phase 0 alone already fixes the most visible gap (a real, documented HTTP integration) with the
least risk; Phases 0-1 together deliver a genuinely complete "serverless HTTP + async messaging"
story matching what AWS Lambda/API Gateway + SQS or Azure Functions + Service Bus already offer -
recommend treating that as the smallest useful v1, same framing the service-mesh roadmap used for
its own Phase 1-2 slice.

## 6. Open questions

1. **Package naming**: `Benzene.Google.*` (matches the existing `examples/Google` precedent) vs.
   `Benzene.GoogleCloud.*` (more explicit - "Google" alone is ambiguous outside a cloud-provider
   context, unlike "Aws"/"Azure" which are unambiguous brand names already). This is a one-way door
   pre-1.0 but a breaking rename post-publish - Azure's own roadmap history shows exactly this
   pain (`Benzene.Azure.Core` → `Benzene.Azure.Function.Core` mid-stream). Recommend deciding this
   explicitly before Phase 0 lands, not after.
2. **Cloud Run vs. Cloud Functions Gen2 as the primary documented deploy target.** Cloud Run is
   architecturally simpler (no Functions Framework dependency at all - just
   `Benzene.AspNet.Core` in a container) and is the direction GCP itself has been steering
   customers. Recommend the getting-started doc lead with Cloud Run and treat Cloud Functions Gen2
   as the "if you specifically want the Functions product" alternative, but this is a genuine
   product-positioning call, not just a technical one.
3. **Push vs. pull Pub/Sub as Phase 1's priority.** Push (CloudEvent HTTP delivery into Cloud
   Functions/Cloud Run) fits a scale-to-zero, serverless-first story matching how AWS
   Lambda/Azure Functions are already positioned; pull requires a long-running process. Recommend
   push first (Phase 1), pull second (Phase 2) - stated as a recommendation, not yet decided.
4. **gRPC on GCP may need no new package at all.** Cloud Run and Cloud Functions Gen2 can both
   serve gRPC, and Benzene already has `Benzene.Grpc`/`Benzene.Grpc.AspNet`. Worth confirming this
   reuses almost unchanged before anyone plans a dedicated `Benzene.Google.Functions.Grpc` package
   that might not need to exist.
5. **Replace vs. keep `examples/Google` as-is.** Given its Pub/Sub half is a non-functional stub, a
   real integration probably shouldn't ship alongside a misleading example. Recommend replacing it
   wholesale once Phase 0-1 land, but flagging since someone already spent recent effort just
   getting the current one to compile (`work/1.0.0-release-status.md`) - worth confirming that
   effort isn't being thrown away for a reason not visible in this pass.
6. **NuGet package versions for `Google.Cloud.Functions.Framework`, `Google.Cloud.PubSub.V1`,
   `Google.Cloud.Tasks.V2`, `Google.Cloud.Firestore`** are deliberately not pinned in this document -
   verify current stable versions at implementation time rather than trusting versions asserted
   here from memory.

## 7. What this document does not cover

Matching the service-mesh roadmap's own convention of stating scope boundaries explicitly:
- No code changes accompany this document - it is planning only, per the request that produced it.
- Detailed IAM/service-account permission tables (the AWS/Azure roadmaps' "IAM/RBAC Permissions
  Reference" appendices) are deferred until package APIs are concrete enough to enumerate exact
  scopes needed - premature to draft against code that doesn't exist yet.
- Cost/performance benchmarking sections (present in the AWS/Azure roadmaps, which cover mature,
  already-deployed packages) are omitted for the same reason - nothing exists yet to benchmark.
- A `google-cloud-product-owner`-style ongoing-ownership role (mirroring `aws-product-owner`/
  `azure-product-owner`) isn't proposed here - out of scope for a planning document, a decision for
  whoever picks this roadmap up to implement.
