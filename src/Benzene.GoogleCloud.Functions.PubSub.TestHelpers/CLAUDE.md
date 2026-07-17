# Benzene.GoogleCloud.Functions.PubSub.TestHelpers

## What this package does
Test-only helpers for exercising a `BenzeneStartUp` through `Benzene.GoogleCloud.Functions.PubSub`
without a live Cloud Functions Framework host or a real Pub/Sub subscription - Phase 1 of
`work/google-cloud-roadmap-1.0.md`. Mirrors `Benzene.GoogleCloud.Functions.Http.TestHelpers`'s
shape and role exactly.

## Key types/interfaces
- `BenzeneTestHostExtensions.BuildGooglePubSubFunctionHost<TStartUp>()` - the
  `Benzene.Testing.BenzeneTestHostBuilder<TStartUp>` bridge. Reconstructs the same
  `GoogleCloudStartUpRunner.Bootstrap` → `ConfigureServices` → `Configure` →
  `GooglePubSubFunctionApplicationBuilder.Build` sequence `GooglePubSubFunctionHost<TStartUp>`
  performs for a real deployment, but via `BenzeneTestHostBuilder.Build(...)` so any
  `WithServices`/`WithConfiguration` overrides are applied before `Configure` runs. Returns a
  private `ICloudEventFunction<MessagePublishedData>` wrapper around the built entry point
  application (Google's own interface has no Benzene-owned equivalent to return directly).
- `BenzeneTestHostExtensions.SendPubSubAsync(ICloudEventFunction<MessagePublishedData>, MessagePublishedData)` -
  wraps the message in a minimal `CloudEvent` envelope (its contents aren't read by anything in the
  pipeline - all Pub/Sub-specific data lives on `MessagePublishedData` itself) and calls `HandleAsync`.
- `PubSubMessageBuilder` - a small `MessagePublishedData` builder (body/attributes/topic/message
  ID/subscription), mirroring `Benzene.GoogleCloud.Functions.Http.TestHelpers.HttpContextBuilder`'s
  shape. `WithTopic(...)` is sugar for `WithAttribute("topic", ...)`, matching
  `PubSubMessageTopicGetter`'s routing convention. Use this for attribute/edge-case-focused tests
  (e.g. a missing `"topic"` attribute) that need finer control than a message builder gives.
- `MessageBuilderExtensions.AsPubSubEvent<T>()` - bridges the shared, repo-wide
  `Benzene.Testing.IMessageBuilder<T>` abstraction (the same one every other transport's
  `.TestHelpers` package extends - e.g. `Benzene.Azure.Function.Kafka.TestHelpers.AsAzureKafkaEvent()`)
  into a `MessagePublishedData`, JSON-serializing the payload and mapping `Topic`/`Headers` onto the
  `"topic"` attribute and the rest of the message's attributes respectively. Prefer this over
  `PubSubMessageBuilder` for ordinary happy-path pipeline tests, to stay consistent with how every
  other transport's tests build their messages.

## When to use this package
- Writing tests for a Google Cloud Functions Pub/Sub-triggered `BenzeneStartUp` that need to
  dispatch a real `MessagePublishedData` through the full pipeline, without starting the Functions
  Framework or a real Pub/Sub subscription.

## Dependencies on other Benzene packages
- **Benzene.GoogleCloud.Functions.PubSub** - `GooglePubSubFunctionApplicationBuilder`.
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, `MicrosoftBenzeneServiceContainer`,
  `MicrosoftServiceResolverFactory`.
- **Benzene.Testing** - `BenzeneTestHostBuilder<TStartUp>`.

## Important conventions
- Test-only: referenced from test projects, not shipped as a runtime dependency.
- Unlike the HTTP test helper's `SendHttpAsync` (which returns the same `HttpContext`, now carrying
  whatever the pipeline wrote to `Response`), `SendPubSubAsync` returns only a completed `Task` -
  there's no response object a Pub/Sub push handler writes to. Assert on handler-visible side
  effects (a test double registered via `WithServices`, a recorded call, etc.), the same way
  `KafkaPipelineTest`/`ServiceBusPipelineTest` assert on their own fire-and-forget transports.
