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
- `IRequestEnricher<TContext>` - Enriches requests with context data
- `IRequestContext<TRequest>` - Provides typed request from context
- `IRequestMapperThunk<TContext>` - Deferred request mapper execution

### Media Formats (`MediaFormats/`)
- `IMediaFormat<TContext>` - A registrable request/response format: content type, whether it can
  read/write for the current context, and its `ISerializer` (replaces the pre-Phase-2 split of
  `ISerializerOption<TContext>`/`ISerializationResponseHandler<TContext>`)
- `IMediaFormatNegotiator<TContext>` - Scoped, memoizing selector that picks the `IMediaFormat<TContext>`
  to read a request with and to write a response with

### Response Handling
- `IResponseHandler<TContext>` - Handles response writing (single async method; the previous
  `ISyncResponseHandler`/`IAsyncResponseHandler` split was removed)
- `IResponseRenderer<TContext>` - One representation a response can be written in (`CanRender` +
  `RenderAsync`); `Benzene.Core.MessageHandlers`' `RendererResponseHandler<TContext>` (an
  `IResponseHandler<TContext>`) walks registered renderers in order, first `CanRender` wins
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
- Response handling is a single async `IResponseHandler<TContext>.HandleAsync` (no sync/async split)
- Handlers are discovered via `IMessageHandlersFinder` (reflection, DI, caching)
- Multiple finders can be composed for layered discovery
- Version selection allows multiple handler versions to coexist
- Transport info provides runtime introspection of available ports
