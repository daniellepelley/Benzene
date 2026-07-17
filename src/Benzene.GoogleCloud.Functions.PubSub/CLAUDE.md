# Benzene.GoogleCloud.Functions.PubSub

## What this package does
Real Pub/Sub push adapter for Benzene on Google Cloud Functions Gen2 - Phase 1 of
`work/google-cloud-roadmap-1.0.md`. Replaces the old, non-functional `examples/Google` Pub/Sub
stub (which logged a hardcoded string and never touched the middleware pipeline at all) with a
package wired through `UseMessageHandlers()` exactly like every other transport, using the same
"topic in a custom attribute" convention `Benzene.Aws.Sqs`/`Benzene.Aws.Lambda.Sqs`/
`Benzene.Aws.Lambda.Sns`/`Benzene.Azure.Function.ServiceBus` already established.

## Key types/interfaces
- `PubSubContext` - wraps a single `Google.Events.Protobuf.Cloud.PubSub.V1.MessagePublishedData`;
  implements `IHasMessageResult`. Unlike AWS/Azure's batch-oriented triggers, Cloud Functions
  Framework's `ICloudEventFunction<TData>` delivers **exactly one** Pub/Sub message per invocation,
  so there's no batch/array context here at all - this is structurally closer to a single HTTP
  request than to Kafka/SNS's per-record batch loop.
- `PubSubMessageTopicGetter` - reads the topic from the message's `"topic"` attribute.
- `PubSubMessageBodyGetter` - reads the message body via `PubsubMessage.TextData` (UTF-8 decode of
  the message's `Data` payload, already provided as a convenience property by the generated type).
- `PubSubMessageHeadersGetter` - exposes the message's attributes as headers.
- `PubSubMessageMessageHandlerResultSetter` - records the outcome onto `PubSubContext.MessageResult`
  (a real setter, not a no-op) - read by `PubSubOptions.RaiseOnFailureStatus`.
- `PubSubMiddlewareApplication` - the per-invocation handler: runs the one message through the
  pipeline, applying `PubSubOptions`. No `Task.WhenAll`/fan-out loop, since there's only ever one
  message to handle.
- `GooglePubSubFunctionHost<TStartUp> : ICloudEventFunction<MessagePublishedData>` - the deploy
  entry point, mirroring `Benzene.GoogleCloud.Functions.Http.GoogleCloudFunctionHost<TStartUp>`'s
  exact bootstrap shape (`GoogleCloudStartUpRunner.Bootstrap<TStartUp>()` →
  `ConfigureServices` → `Configure` → build).
- `GooglePubSubFunctionApplicationBuilder` - the deferred-build `IBenzeneApplicationBuilder`
  `Configure` runs against. Unlike the HTTP package's builder (which implements
  `Benzene.AspNet.Core.IAspApplicationBuilder` to piggyback on existing ASP.NET Core machinery),
  this one is fully self-contained - there's no existing Benzene abstraction for a CloudEvent
  trigger to reuse, so `UsePubSub` recognizes it directly via `is GooglePubSubFunctionApplicationBuilder`.
- `PubSubOptions` / `PubSubMessageProcessingException` - configurable exception/failure-status
  handling, same `CatchExceptions`/`RaiseOnFailureStatus` shape used by
  `Benzene.Azure.Function.Kafka`/`Benzene.Azure.Function.ServiceBus`.
- `DependencyInjectionExtensions.AddGooglePubSub()` / `UsePubSub(...)` - registration and pipeline
  wiring, called from `BenzeneStartUp.Configure`.

## When to use this package
- Consuming Pub/Sub messages via a **push** subscription delivered to a Cloud Functions Gen2
  CloudEvent trigger. For a **pull** subscription (a long-running `SubscriberClient` on GKE/Compute
  Engine/an always-on Cloud Run instance), see Phase 2 of the roadmap - not built yet.

## Dependencies on other Benzene packages
- **Benzene.GoogleCloud.Functions.Core** - `GoogleCloudStartUpRunner.Bootstrap<TStartUp>()`.
- **Benzene.Core.MessageHandlers** - `TransportMiddlewarePipeline<TContext>`, `JsonSerializer`,
  the getter/setter interfaces this package implements.
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, `MicrosoftServiceResolverFactory`.
- **Google.Cloud.Functions.Framework** / **Google.Events.Protobuf** - `ICloudEventFunction<TData>`,
  `MessagePublishedData`/`PubsubMessage`.

## Important conventions
- Exception/failure-status handling is configurable via `PubSubOptions` (`UsePubSub(..., configure)`),
  defaulting to today's natural behavior for a CloudEvent trigger: a handler exception cascades and
  fails the invocation (Cloud Functions Framework turns an unhandled exception into a non-2xx
  response, so the subscription's own retry/dead-letter policy notices), and a non-exception failure
  result is silently accepted. Set `PubSubOptions.CatchExceptions = true` to catch and log an
  exception instead; set `PubSubOptions.RaiseOnFailureStatus = true` to escalate a non-exception
  failure result into a thrown `PubSubMessageProcessingException` too. Both default to `false`.
- **Preset-topic override is not implemented for this package yet** - unlike
  `Benzene.Aws.Sqs`/`Benzene.Aws.Lambda.Sqs`/`Benzene.Azure.Function.ServiceBus`, there's no
  `UsePresetTopic()` wiring here. A subscription whose producer never sets a `"topic"` attribute
  will route as `Missing` today. Adding it is a small, additive follow-up (register
  `PresetTopicMessageTopicGetter<PubSubContext>` decorating the real getter, matching the other
  three transports' DI registration exactly) - not done in this pass to keep Phase 1 scoped to what
  the roadmap actually called for.
- No `examples/Google` wiring yet - the roadmap treats that as Phase 5 packaging/docs work, not part
  of shipping the adapter itself.

## Tests
- `test/Benzene.Core.Test/Google/PubSubPipelineTest.cs` - full pipeline happy path (real
  `MessagePublishedData` through `.UsePubSub().UseMessageHandlers()`).
- `test/Benzene.Core.Test/Google/PubSubGettersTest.cs` - `PubSubMessageBodyGetter`,
  `PubSubMessageTopicGetter` (including the missing-attribute case), `PubSubMessageHeadersGetter`.
- `test/Benzene.Core.Test/Google/PubSubFailureHandlingTest.cs` - `PubSubOptions`'
  `CatchExceptions`/`RaiseOnFailureStatus` combinations against `PubSubMiddlewareApplication` directly.
