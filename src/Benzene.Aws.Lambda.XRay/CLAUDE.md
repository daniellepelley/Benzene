# Benzene.Aws.Lambda.XRay

## What this package does
Per-middleware tracing straight into **AWS X-Ray**, with no OpenTelemetry/OTLP collector in the middle.
`AddXRayTracing()` wraps every middleware in every pipeline in an X-Ray **subsegment** (via the AWS
X-Ray SDK, `AWSXRayRecorder.Instance`), named after the middleware. Because the X-Ray SDK attaches to
the Lambda's ambient segment (populated per invocation from `_X_AMZN_TRACE_ID` when active tracing is
on), those subsegments **nest directly under the `AWS::Lambda::Function` segment** — so the middleware
breakdown appears inside the same X-Ray trace as the AWS-level segments.

This is the modern reintroduction of the old **`Benzene.Aws.XRay`** package (its `UseXRayTracing()` /
`XRayProcessTimerFactory`), which was deleted in the 1.0 observability rework on the assumption that
OpenTelemetry would cover everything. It doesn't cover this specific case cleanly: OTel spans carry
their own W3C trace ids, so exported to X-Ray via a collector they land as *separate* traces rather than
nested under the Lambda segment. Going straight through the X-Ray SDK is what puts the middleware
timeline where an X-Ray user expects it. See the AwsMesh example.

## Key types
- `XRayMiddlewareWrapper : IMiddlewareWrapper` / `XRayMiddlewareDecorator<TContext>` — the X-Ray twin of
  `Benzene.Diagnostics`'s `ActivityMiddlewareWrapper`/`ActivityMiddlewareDecorator`. Opens a subsegment
  per stage, annotates it (`benzene_transport`/`benzene_topic`/`benzene_version`/`benzene_handler` —
  underscores because X-Ray rejects dots in annotation keys), records exceptions as a fault on the
  failing stage, and closes the subsegment in a `finally`. **`benzene_transport` is only annotated once a
  transport is resolved** — while `ICurrentTransport.Name` still reads `TransportNames.Unresolved`
  (`"<missing>"`), the tag is skipped, so the outer probe stages a multi-transport function walks (an SQS
  handler declining an SNS event, say) aren't stamped with the sentinel. `ActivityMiddlewareDecorator`
  does the same for its `benzene.transport` tag.
- `DependencyInjectionExtensions.AddXRayTracing()` — registers the wrapper (with the same
  `IsTypeRegistered` guard the Activity one uses, so it never double-wraps). The modern name for the old
  `UseXRayTracing()`, and the X-Ray equivalent of `AddActivityPerMiddleware()`.

## Important conventions / behaviour
- **No-op off Lambda.** With no active X-Ray segment in context (`BeginSubsegment` throws
  `EntityNotAvailableException`), the decorator runs the inner middleware untraced — same shape as
  `ActivitySource.StartActivity` returning `null` with no listener. So it is safe to wire everywhere;
  it only does anything on Lambda with active tracing on.
- **Composes with `AddDiagnostics()`/`AddActivityPerMiddleware()`.** Both register `IMiddlewareWrapper`s
  and the pipeline applies all of them, so a service can emit X-Ray subsegments *and* OTel `Activity`
  spans at once. Wire only this one if X-Ray is your only backend.
- **No exporter/collector required.** The AWS X-Ray SDK posts subsegments to the X-Ray daemon the Lambda
  runtime already provides when active tracing is enabled (`tracing_config { mode = "Active" }`), which
  needs the `xray:PutTraceSegments`/`PutTelemetryRecords` IAM (the `AWSXRayDaemonWriteAccess` policy).

## Dependencies
- **Benzene.Abstractions.Middleware** — `IMiddleware`/`IMiddlewareWrapper`/`IServiceResolver`.
- **Benzene.Abstractions.MessageHandlers** — `ICurrentTransport`/`IMessageGetter<TContext>`/
  `IMessageHandlerDefinitionLookUp` for the subsegment annotations.
- **AWSXRayRecorder.Core** — the AWS X-Ray SDK (same version Benzene.Tools' test host already uses).

## Testing
`test/Benzene.Aws.Tests/XRayMiddlewareTest.cs` mirrors `ActivityMiddlewareTest`: a subsegment per named
middleware, idempotent registration, exception recorded as a fault + rethrown, and the off-Lambda no-op.
Tests force a **sampled** segment (`new SamplingResponse(SampleDecision.Sampled)`) because the default
X-Ray sampling strategy rate-limits — back-to-back unsampled test segments would drop their subsegments.
