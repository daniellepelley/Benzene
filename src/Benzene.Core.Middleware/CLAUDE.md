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
- `EntryPointMiddlewareApplication<TEvent>` - Entry point wrapper
- `EntryPointMiddlewareApplication<TEvent, TResult>` - Entry point wrapper with result

### Middleware Components
- `FuncWrapperMiddleware<TContext>` - Wraps Func as middleware
- `ContextConverterMiddleware<TContext, TContextOut>` - Converts context types
- `MiddlewareRouter` - Routes to different pipelines

### Context Conversion
- `InlineContextConverter<TIn, TOut>` - Inline context transformation

### Null Implementations
- `NullBenzeneServiceContainer` - Null object pattern container
- `NullServiceResolver` - Null object pattern resolver
- `NullServiceResolverFactory` - Null object pattern factory

### Extension Methods (Extensions.cs)
- `.Use()` - Adds middleware to pipeline
- `.OnRequest()` - Executes action on request
- `.OnResponse()` - Executes action on response
- `.Split()` - Splits pipeline into branches
- `.Convert()` - Converts context type
- Many more fluent configuration methods

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
- Pipeline builder is immutable - each call returns new builder instance
- Async/await used throughout for I/O-bound operations
