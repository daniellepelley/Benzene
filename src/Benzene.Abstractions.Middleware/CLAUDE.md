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
