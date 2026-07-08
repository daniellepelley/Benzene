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
- `MultiSerializerOptionsRequestMapper<TContext, TDefaultSerializer>` - Multi-serializer support
- `JsonDefaultMultiSerializerOptionsRequestMapper<TContext>` - JSON default multi-serializer
- `EnrichingRequestMapper<TContext>` - Enriches requests with context data
- `SerializerOptionBase` - Base class for serializer options
- `RequestMapperThunk<TContext>` - Deferred request mapping

### Response Handling
- `ResponseHandler<T, TContext>` - Base response handler
- `ResponseHandlerContainer<TContext>` - Contains response handlers
- `ResponseBodyHandler<TContext>` - Handles response body
- `DefaultResponsePayloadMapper<TContext>` - Maps response payloads
- `ResponseIfHandledMessageHandlerResultSetter<TContext>` - Sets result if handled
- `ResponseMessageMessageHandlerResultSetterBase<TContext>` - Base for result setters
- `DefaultMessageMessageHandlerResultSetterBase` - Default result setter base

### Serialization
- `JsonSerializer` - System.Text.Json implementation
- `PayloadSerializer` - Serializes payloads
- `BodySerializer<TContext>` - Serializes response bodies
- `IBodySerializer` - Body serializer abstraction
- `ISerializationResponseHandler<TContext>` - Serialization response handler
- `JsonSerializationResponseHandler<TContext>` - JSON response handler

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
- Multiple serializers can coexist via `ISerializerOption<TContext>`
- Request mapping separates transport concerns from handler logic
- Response handling is async-first but supports sync handlers
- BenzeneMessage is the transport-agnostic message format
- Filters run before handlers and can short-circuit execution
- Handler versioning allows multiple versions to coexist
- Default status codes map to HTTP conventions but work with any transport
- JsonSerializer uses System.Text.Json (can be replaced via DI)
