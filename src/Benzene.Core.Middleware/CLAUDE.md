# Benzene.Core.Middleware

## What this package does
Provides concrete implementations of Benzene's middleware pipeline system. Includes pipeline builders, middleware applications, context converters, and a rich set of extension methods for composing middleware chains. This is the runtime engine for processing requests through middleware.

## Key types/interfaces

### Pipeline Implementation
- `MiddlewarePipeline<TContext>` - Executes middleware chain
- `MiddlewarePipelineBuilder<TContext>` - Fluent builder for pipelines
- `DefaultMiddlewareFactory` - Default factory using DI container

### Application Entry Points
- `MiddlewareApplication<TEvent, TContext>` - Single-event application
- `MiddlewareApplication<TEvent, TContext, TResult>` - Single-event with result
- `MiddlewareMultiApplication<TEvent, TContext>` - Multi-event application
- `MiddlewareMultiApplication<TEvent, TContext, TResult>` - Multi-event with result
- `BoundedFanOut` - the shared helper both `MiddlewareMultiApplication` overloads (and every
  hand-rolled batch fan-out app - SQS/SNS/ServiceBus/Azure-Kafka) route their per-record concurrency
  through. `WhenAllAsync(source, body, maxDegreeOfParallelism)`: `null`/`<=0` runs every record at
  once (the original `Select(...).ToArray()` + `Task.WhenAll` behavior), a positive value caps how
  many run concurrently via a `SemaphoreSlim`. Results come back in source order regardless of the
  cap or completion order, so a caller's positional/`Where(x => x != null)` filtering is unaffected.
  This is the opt-in `MaxDegreeOfParallelism` knob's implementation - it exists so a large batch
  can't start hundreds of pipeline runs (and hundreds of scoped DB connections) simultaneously.
  `MiddlewareMultiApplication` exposes it as an optional `maxDegreeOfParallelism` constructor arg
  (default `null` = unbounded), so the delegating transports (S3, Lambda-Kafka, EventHub, EventGrid,
  QueueStorage) thread it through their own constructor/`Use*` param; the transports with an options
  object (SQS `SqsOptions`, SNS `SnsOptions`, `SqsConsumerOptions`, `ServiceBusOptions`, Azure
  `KafkaOptions`) expose it as `MaxDegreeOfParallelism` on that options type instead. Covered by
  `test/Benzene.Core.Test/Core/Middleware/BoundedFanOutTest.cs` +
  `MiddlewareMultiApplicationConcurrencyTest.cs`.
- `EntryPointMiddlewareApplication<TEvent>` - Entry point wrapper
- `EntryPointMiddlewareApplication<TEvent, TResult>` - Entry point wrapper with result

**Both `MiddlewareApplication<...>` overloads create one new DI scope per `HandleAsync` call
(`serviceResolverFactory.CreateScope()`) and dispose it in a `using` once the pipeline finishes**
(fixed - previously the created scope was never disposed, leaking one scope, and any scoped
`IDisposable` resolved inside it, per event/message, forever - a real, standing resource leak
affecting every concrete subclass: `AspNetApplication` (both `Benzene.AspNet.Core`'s and
`Benzene.Azure.Function.AspNet`'s), `Benzene.Kafka.Core.KafkaApplication<TKey,TValue>`,
and `BenzeneMessageApplication`, which in turn is
used by AWS Lambda's `DirectMessageLambdaHandler` and Azure Event Hub's
`BenzeneMessageEventHubHandler`). `MiddlewareMultiApplication<...>` (the batch-oriented sibling)
already disposed its per-record scopes correctly and was not affected. Regression coverage:
`test/Benzene.Core.Test/Core/Middleware/MiddlewareApplicationScopeDisposalTest.cs`. This pairs with
the earlier fix to `MicrosoftServiceResolverAdapter`/`AutofacServiceResolverAdapter` (both adapters'
own `Dispose()` used to be a no-op - see
`test/Benzene.Core.Test/Core/Core/DI/ServiceResolverScopeDisposalTest.cs`) to close the leak
end to end: the scope is both disposed now, and disposing it actually releases scoped services.

**Ambient cancellation token seeding.** Both `MiddlewareApplication<...>` overloads also have a
`HandleAsync(TEvent, IServiceResolverFactory, CancellationToken)` overload that seeds the per-event
scope's `Benzene.Core.CancellationTokenAccessor` (via `IServiceResolver.SeedCancellationToken(...)`,
a no-op for `CancellationToken.None`) right after `CreateScope()`, so any component resolving
`ICancellationTokenAccessor` during the pipeline observes the transport's real cancellation signal
without a signature change to the pipeline/handlers (the scoped-accessor pattern, like
`PresetTopicHolder`). The original no-token overload delegates with `CancellationToken.None`.
Transports seed it where they have a signal: **per-message/-call** - `Benzene.AspNet.Core` +
`Benzene.Azure.Function.AspNet` (`HttpContext.RequestAborted`, via a `SeedCancellationToken`
middleware), `Benzene.Grpc` (`ServerCallContext.CancellationToken`), `Benzene.RabbitMq`
(`BasicDeliverEventArgs.CancellationToken`), the `Benzene.Azure.ServiceBus`/`Benzene.Azure.EventHub`
workers (`ProcessMessageEventArgs`/`ProcessEventArgs.CancellationToken`, via the token overload); and
**worker-shutdown** - `Benzene.Kafka.Core` (the worker's linked run token).
AWS Lambda (no `ILambdaContext` token) and the Azure Functions non-HTTP triggers (their
`FunctionContext.CancellationToken` never reaches Benzene) have no signal to seed;
`Benzene.GoogleCloud.Functions.PubSub` has one but seeding it needs a token overload on
`IEntryPointMiddlewareApplication` (a public-interface change) - deferred. Covered by
`test/Benzene.Core.Test/Core/Middleware/CancellationTokenSeedingTest.cs`.

### Middleware Components
- `FuncWrapperMiddleware<TContext>` - Wraps Func as middleware
- `ContextConverterMiddleware<TContext, TContextOut>` - Converts context types
- `MiddlewareRouter<TRequest, TContext>` - abstract base for routing to different pipelines. Its
  `Name` (used in tracing) defaults to the concrete router's own type name (`GetType().Name`), not a
  fixed `"MiddlewareRouter"`, so each flavour (`SqsLambdaHandler`, `ApiGatewayLambdaHandler`, ...) is
  distinguishable in traces without any per-inheritor change; `Name` is `virtual` so an inheritor can
  still override it
- `ExceptionHandlerMiddleware<TContext>` - Centralized exception handling
- `StreamContext<TItem>` / `StreamMiddlewareApplication<...>` / `IStreamCheckpointer<TItem>` /
  `NullStreamCheckpointer<TItem>` - stream-record processing (used by `.UseStream()`)
- `BenzeneApplicationBuilder` / `BenzeneInvocation` / `BenzeneInvocationAccessor` - the concrete
  hosting app-builder and per-invocation context implementations

### Context Conversion
- `InlineContextConverter<TIn, TOut>` - Inline context transformation

### Null Implementations
- `NullBenzeneServiceContainer` - Null object pattern container
- `NullServiceResolver` - Null object pattern resolver
- `NullServiceResolverFactory` - Null object pattern factory

### Extension Methods (Extensions.cs and siblings)
- `.Use()` - Adds middleware to pipeline
- `.OnRequest()` - Executes action on request
- `.OnResponse()` - Executes action on response
- `.Split()` - Splits pipeline into branches
- `.Convert()` - Converts context type
- `.UseExceptionHandler()` - Adds exception handling middleware
- `.UseLogContext()` / `.UseLogResult()` - log-scope enrichment (via `LoggerExtensions`)
- `.UseBenzeneInvocation()` - populates the per-invocation `IBenzeneInvocation`
  (`BenzeneInvocationExtensions`)
- `.UseStream()` - stream-processing pipeline (`StreamExtensions`)

### Other
- `RegisterDependency` - Base class for registration modules
- `Constants` - Framework constants
- `DependencyExtensions` - DI-related extensions
- `LoggerExtensions` - Logging extensions

## When to use this package
- When building transport adapters (HTTP, Lambda, Kafka, etc.)
- When creating applications with middleware pipelines
- When you need to compose request processing logic
- This is typically used via transport-specific packages (AspNet.Core, Aws.Lambda.*)

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Uses DI, logging, and result abstractions
- **Benzene.Abstractions.Middleware** - Implements middleware interfaces
- **Benzene.Core** - Uses core utilities and logging

## Important conventions
- Middleware executes in registration order via `.Use()`
- `.OnRequest()` and `.OnResponse()` provide tap points without custom middleware
- `.Split()` allows conditional branching in pipeline
- `.Convert()` changes context type between pipeline stages
- `FuncWrapperMiddleware` enables inline middleware without custom classes
- `MiddlewareMultiApplication` handles multiple event types in one application
- Context converters must be explicitly registered between pipeline stages
- Pipeline builder is **mutable and fluent**: `.Use()`/`.OnRequest()`/`.OnResponse()`/etc. append to
  a shared list and return the same builder (`this`) - do NOT fork one builder into divergent
  pipelines (a second `.Use()` on a "base" builder mutates the base, not a copy). Only `Create<T>()`
  allocates a fresh builder; `Build()` snapshots the items at call time. (Internal `.Split()`/
  `.Convert()` are safe because they call `Create<T>()` for each branch.)
- Async/await used throughout for I/O-bound operations
