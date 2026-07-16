# Benzene.Abstractions.Middleware

## What this package does
Defines core middleware abstractions for Benzene's pipeline architecture. Provides interfaces for building composable middleware pipelines that process requests through a chain of components. This is the foundation for hexagonal architecture port adapters.

## Key types/interfaces

### Core Middleware
- `IMiddleware<TContext>` - Base middleware interface with contravariant context
- `IMiddlewarePipeline<TContext>` - Executes a pipeline of middleware
- `IMiddlewarePipelineBuilder<TContext>` - Fluent builder for constructing pipelines

### Application Entry Points
- `IMiddlewareApplication<TEvent>` - Entry point for event-driven applications
- `IMiddlewareApplication<TRequest, TResponse>` - Entry point for request/response applications
- `IEntryPointMiddlewareApplication` - Marker interface for entry points
- `IEntryPointMiddlewareApplication<TEvent>` - Typed entry point (event only)
- `IEntryPointMiddlewareApplication<TEvent, TResult>` - Typed entry point (event with result)

### Infrastructure
- `IMiddlewareFactory` - Factory for creating middleware instances
- `IMiddlewareWrapper` - Marker interface for middleware wrappers

### Utilities
- `IContextConverter<TIn, TOut>` - Transforms context between pipeline stages
- `IContextPredicate<TContext>` - Conditional routing/branching in pipelines

## When to use this package
- When building new transport adapters (HTTP, Lambda, Kafka, etc.)
- When creating custom middleware components
- When implementing hexagonal architecture ports
- When you need to process requests through a composable pipeline
- Rarely used directly in application code - consumed by concrete implementations

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Uses result types and DI abstractions

## Important conventions
- `IMiddleware<in TContext>` uses contravariance (`in`) for flexible context composition
- Middleware executes in registration order (first registered = first executed)
- Each middleware can short-circuit the pipeline by not calling `next()`
- Pipeline builders follow fluent API pattern
- Context conversion allows splitting pipelines into stages with different context types
- Entry point applications are the top-level bootstrap for middleware pipelines

### Context purity - prefer scoped DI state over extending the context
A `TContext` type should describe one thing: the shape of a transport message (its headers, body,
event source, etc.) plus whatever result-tracking a transport genuinely needs (`IHasMessageResult`
and similar). It should **not** grow marker interfaces or settable properties whose only purpose is
to let one specific middleware pass a value to a later step in one specific pipeline. That couples
every context type, present and future, to every optional cross-cutting feature that happens to
exist, and it leaks a middleware-internal concern into a type application code also sees.

The seam for "middleware A needs to hand a value to component B, later in the same pipeline, for
this one message" is **scoped DI state**, not the context:
1. Define a small plain class to hold the value (no interface needed unless app code is meant to
   consume it directly) - e.g. `PresetTopicHolder { public ITopic? PresetTopic { get; set; } }`.
2. Register it `services.TryAddScoped<TheHolder>()` in the transport's own DI extension (or
   centrally, if it's truly transport-agnostic). A fresh instance is created per message, because a
   fresh DI scope is created per message everywhere in this codebase (`IServiceResolverFactory.CreateScope()`).
3. Middleware A resolves the holder from the `IServiceResolver` its `Use(resolver => ...)` factory
   already receives, and mutates it.
4. Component B (a decorator around the transport's real implementation, a later middleware,
   whatever) takes the same holder type as a constructor dependency, resolved from the same scope.

This works correctly even when the underlying interface (e.g. `IMessageTopicGetter<TContext>`) is
registered **once**, shared across multiple pipelines/queues using the same `TContext` - which is
the norm, since `IBenzeneServiceContainer` registrations are shared app-wide, not pipeline-scoped.
The shared registration doesn't need to know which pipeline is running; it just reads whatever the
current message's scoped holder contains, and that holder is `null`/default unless the *specific*
pipeline that opted in actually ran its middleware for *this* message. A pipeline that never adds
the opt-in middleware behaves identically to a codebase that never had the feature at all - no
context coupling, no per-queue DI registration conflicts.

Correctness relies on one property of `MiddlewarePipeline<TContext>` (see
`Benzene.Core.Middleware/MiddlewarePipeline.cs`): middleware factories run fresh per message using
that message's own scoped resolver, and middleware runs in registration order - so a mutation by an
earlier middleware always happens-before a read by a later step in the same `next()` chain, exactly
the same guarantee context mutation relied on, without the coupling.

See `Benzene.Core.MessageHandlers`' `PresetTopicHolder`/`PresetTopicMiddleware<TContext>`/
`PresetTopicMessageTopicGetter<TContext>` for the worked example, and that package's `CLAUDE.md`.
