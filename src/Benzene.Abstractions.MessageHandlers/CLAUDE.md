# Benzene.Abstractions.MessageHandlers

## What this package does
Defines abstractions for message-based request/response handling in Benzene. Provides interfaces for message routing, handler discovery, request mapping, response handling, and transport-agnostic message processing. This is the foundation for command/query handlers in hexagonal architecture.

## Key types/interfaces

### Core Handlers
- `IMessageHandler` - Base marker interface for all message handlers
- `IMessageHandler<TRequest>` - Handler with request, no response
- `IMessageHandler<TRequest, TResponse>` - Handler with request and response
- `IMessageHandlerBase<TRequest, TResponse>` - Base interface with context

### Handler Infrastructure
- `IMessageHandlerFactory` - Creates handler instances
- `IMessageHandlersList` - Collection of registered handlers
- `IMessageHandlersFinder` - Discovers handlers (reflection, cache, etc.)
- `IMessageHandlerDefinition` - Metadata about a handler
- `IMessageHandlerDefinitionLookUp` - Looks up handler definitions by topic

### Context & Results
- `IBenzeneMessageContext` - Base context for message handling
- `IMessageHandlerResult` - Non-generic result from handler
- `IMessageHandlerResult<TResponse>` - Generic result with response payload
- `IHasMessageResult` - Context that contains message result

### Pipelines
- `IHandlerPipelineBuilder` - Builds middleware pipeline around handlers
- `IHandlerMiddlewareBuilder` - Builds handler-specific middleware
- `IPipelineMessageHandler<TRequest, TResponse>` - Handler wrapped in pipeline

### Request Mapping
- `IRequestMapper<TContext>` - Maps transport context to request objects
- `IRequestMapBuilder<TContext>` - Builds request mapping configuration
- `IRequestEnricher<TContext>` - Enriches requests with context data
- `IRequestContext<TRequest>` - Provides typed request from context
- `ISerializerOption<TContext>` - Selects serializer based on context
- `IRequestMapperThunk<TContext>` - Deferred request mapper execution

### Response Handling
- `IResponseHandler<TContext>` - Handles response writing
- `IAsyncResponseHandler<TContext>` - Async response handling
- `ISyncResponseHandler<TContext>` - Sync response handling
- `IResponseHandlerContainer<TContext>` - Contains response handlers
- `IBenzeneResponseAdapter<TContext>` - Adapts responses to transport format
- `IResponsePayloadMapper<TContext>` - Maps response payloads
- `IMessageHandlerResultSetter<TContext>` - Sets result on context

### Routing
- `IMessageRouterBuilder` - Builds message routing configuration
- `IVersionSelector` - Selects handler version

### Message Extraction
- `IMessageGetter<TContext>` - Extracts message from transport context
- `IMessageTopicGetter<TContext>` - Extracts topic/routing key from context

### Transport Info
- `ITransportsInfo` - Information about available transports
- `ITransportInfo` - Information about a specific transport
- `ICurrentTransport` - Gets current transport being used
- `ISetCurrentTransport` - Sets current transport
- `IApplicationInfo` - Application-level metadata

### Other
- `IMessageHandlerWrapper` - Wraps handlers with additional behavior
- `IMessageResult` - Legacy result interface

## When to use this package
- When implementing custom message transport adapters
- When creating handler discovery mechanisms
- When building custom request/response mapping logic
- When implementing transport-specific message routing
- Rarely used directly in application code - consumed by Core.MessageHandlers

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Uses result types and DI abstractions
- **Benzene.Abstractions.Middleware** - Message handlers run within middleware pipelines

## Important conventions
- Handler topic/routing is determined by `IMessageHandlerDefinition`
- Request mapping separates transport concerns from handler logic
- Response handling is split into sync/async for flexibility
- Handlers are discovered via `IMessageHandlersFinder` (reflection, DI, caching)
- Multiple finders can be composed for layered discovery
- Version selection allows multiple handler versions to coexist
- Transport info provides runtime introspection of available ports
