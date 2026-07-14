# Benzene.Core.MessageHandlers

## What this package does
Provides complete implementation of message handler infrastructure for command/query/event handling. Includes handler discovery (reflection, DI, caching), request mapping, response handling, routing, serialization, and the BenzeneMessage abstraction for transport-agnostic messaging.

## Key types/interfaces

### Core Handler Implementations
- `MessageHandler<TRequest, TResponse>` - Base class for typed handlers
- `PipelineMessageHandler<TRequest, TResponse>` - Handler wrapped in middleware pipeline
- `MessageHandlerMiddleware<TRequest, TResponse>` - Middleware that invokes handlers
- `MessageHandlerNoResultWrapper<TRequest, TResponse>` - Wraps handlers with no result
- `PipelineMessageHandlerWrapper` - Wraps pipeline handlers

### Results
- `MessageHandlerResult` - Non-generic result implementation
- `MessageHandlerResult<TResponse>` - Generic result with response
- `MessageResult` - Legacy result type
- `DefaultStatuses` - Standard HTTP/message status codes

### Handler Discovery
- `ReflectionMessageHandlersFinder` - Discovers handlers via reflection
- `DependencyMessageHandlersFinder` - Discovers handlers from DI container
- `CacheMessageHandlersFinder` - Caches discovered handlers
- `CompositeMessageHandlersFinder` - Combines multiple finders
- `MessageHandlersList` - List of discovered handlers
- `MessageHandlerDefinition` - Metadata about a handler
- `MessageHandlerDefinitionLookUp` - Looks up handlers by topic

### Request Mapping
- `RequestMapper<TContext>` - Maps transport context to request
- `MultiSerializerOptionsRequestMapper<TContext>` - Negotiator-driven request mapper; asks the
  registered `IMediaFormatNegotiator<TContext>` which format to read with, then caches the
  resulting `EnrichingRequestMapper`/`RequestMapper` pair per distinct `ISerializer`
- `EnrichingRequestMapper<TContext>` - Enriches requests with context data
- `RequestMapperThunk<TContext>` - Deferred request mapping

### Media Formats (`MediaFormats/`)
- `JsonMediaFormat<TContext>` - The process default `IMediaFormat<TContext>`, wraps the shared
  `JsonSerializer` singleton
- `AcceptHeaderMediaFormatBase<TContext>` - Base for header-negotiated formats (`content-type` for
  reads, `accept` for writes), e.g. `Benzene.Xml`'s `XmlMediaFormat`
- `MediaFormatNegotiator<TContext>` - Default `IMediaFormatNegotiator<TContext>`; scoped and
  memoizing (one selection per message)
- `AddMediaFormatNegotiation<TContext>()` - DI extension every transport (and `AddContextItems()`)
  calls to register the default format + negotiator for a context type

### Response Handling
- `SerializationResponseHandler<TContext>` - The single response-writing handler: asks the
  negotiator for the write format and writes body + content type, unless a body is already set
- `ResponseHandlerContainer<TContext>` - Contains response handlers
- `DefaultResponsePayloadMapper<TContext>` - Maps response payloads
- `ResponseIfHandledMessageHandlerResultSetter<TContext>` - Sets result if handled
- `ResponseMessageMessageHandlerResultSetterBase<TContext>` - Base for result setters
- `DefaultMessageMessageHandlerResultSetterBase` - Default result setter base

### Serialization
- `JsonSerializer` - System.Text.Json implementation
- `PayloadSerializer` - Serializes payloads

### BenzeneMessage (Transport-Agnostic)
- `BenzeneMessageApplication` - Application for BenzeneMessage
- `BenzeneBodyMapper` - Maps BenzeneMessage bodies
- `BenzeneMessageMessageHandlerResultSetter` - Sets BenzeneMessage results
- `BenzeneMessageResponseAdapter` - Adapts responses to BenzeneMessage
- `DefaultResponseStatusHandler<TContext>` - Handles response status

### Context
- `BenzeneMessageContext` - Context for BenzeneMessage handling
- `MessageHandlerContext<TRequest, TResponse>` - Generic handler context

### Routing
- `MessageRouter<TContext>` - Routes messages to handlers
- `MessageRouterBuilder` - Builds message routing
- `HandlerPipelineBuilder` - Builds handler pipelines
- `VersionSelector` - Selects handler versions

### Transport Info
- `ApplicationInfo` - Application metadata
- `BlankApplicationInfo` - Empty application info
- `TransportInfo` - Transport metadata
- `TransportsInfo` - Collection of transports
- `CurrentTransportInfo` - Current transport context
- `TransportMiddlewarePipeline<TContext>` - Transport-specific pipeline

### Filters
- `IFilter<T>` - Filter abstraction
- `FiltersMiddleware<TRequest, TResponse>` - Middleware for filters
- `FiltersMiddlewareBuilder` - Builds filter middleware

### Other
- `MessageHandlerFactory` - Creates handler instances
- `MessageAttribute` - Attribute for marking message handlers
- `MessageGetter<TContext>` - Gets messages from context
- `CoreRegistrations` - Registers core services
- Various extension methods in `Extensions.cs`, `MessageMapperExtensions.cs`

## When to use this package
- When building applications with command/query handlers
- When implementing transport adapters (Lambda, Kafka, HTTP)
- When you need message routing and handler discovery
- Typically used via transport-specific packages (Aws.Lambda.*, Azure.*, etc.)

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Uses core abstractions
- **Benzene.Abstractions.Middleware** - Uses middleware abstractions
- **Benzene.Abstractions.MessageHandlers** - Implements message handler interfaces
- **Benzene.Core** - Uses core utilities
- **Benzene.Core.Middleware** - Uses middleware infrastructure

## Important conventions
- Handlers are discovered using `[Message("topic")]` attribute or naming conventions
- Handler discovery is cached for performance via `CacheMessageHandlersFinder`
- Multiple formats can coexist via `IMediaFormat<TContext>`, chosen per-message by
  `IMediaFormatNegotiator<TContext>` (`content-type` for reads, `accept` for writes)
- Request mapping separates transport concerns from handler logic
- Response handling is a single async `IResponseHandler<TContext>.HandleAsync`
- BenzeneMessage is the transport-agnostic message format
- Filters run before handlers and can short-circuit execution
- Handler versioning allows multiple versions to coexist
- Default status codes map to HTTP conventions but work with any transport
- JsonSerializer uses System.Text.Json (can be replaced via DI)
