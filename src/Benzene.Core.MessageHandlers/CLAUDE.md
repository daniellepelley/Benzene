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
- `RequestMapper<TContext>` - Maps transport context to request; prefers the byte-oriented path
  (deserializing straight from an `IMessageBodyBytesGetter<TContext>`'s bytes) when the selected
  `ISerializer` implements `IPayloadSerializer` and that getter is registered for `TContext`,
  otherwise falls back to the string path unchanged. `BenzeneMessageContext` is the reference
  transport wired for this (`BenzeneMessageGetter` implements `IMessageBodyBytesGetter`).
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
- `RendererResponseHandler<TContext>` - The single response-writing `IResponseHandler<TContext>`
  every transport registers: short-circuits if a body is already set, otherwise walks registered
  `IResponseRenderer<TContext>`s in order and delegates to the first whose `CanRender` matches
- `SerializerResponseRenderer<TContext>` - The catch-all `IResponseRenderer<TContext>` (registered
  last): asks the negotiator for the write format and writes body + content type; honors
  `IRawContentMessage.ContentType` when the handler's payload implements it
- `ResponseHandlerContainer<TContext>` - Contains response handlers
- `DefaultResponsePayloadMapper<TContext>` - Maps response payloads
- `ResponseIfHandledMessageHandlerResultSetter<TContext>` - Sets result if handled
- `ResponseMessageHandlerResultSetterBase<TContext>` - Base for result setters
- `DefaultMessageHandlerResultSetterBase<TContext>` - Default result setter base
- `MessageHandlerResultSetterBase<TContext>` - shared result-setter base

### Serialization
- `JsonSerializer` - System.Text.Json implementation of `ISerializer`; also implements
  `IPayloadSerializer` (byte-oriented, via `Utf8JsonWriter`/`Utf8JsonReader`), so it's used on the
  byte path whenever an `IMessageBodyBytesGetter<TContext>` is registered for the context

### BenzeneMessage (Transport-Agnostic)
- `BenzeneMessageApplication` - Application for BenzeneMessage
- `BenzeneMessageGetter` - reads topic/headers/body off a `BenzeneMessageContext` and implements
  `IMessageBodyBytesGetter` (declared in `BenzeneBodyMapper.cs`, but the class name is
  `BenzeneMessageGetter`)
- `BenzeneMessageHandlerResultSetter` - Sets BenzeneMessage results
- `BenzeneMessageResponseAdapter` - Adapts responses to BenzeneMessage
- `DefaultResponseStatusHandler<TContext>` - Handles response status

### Context
- `MessageHandlerContext<TRequest, TResponse>` - Generic handler context (the concrete
  `IMessageHandlerContext<TRequest, TResponse>`). Note: the `BenzeneMessageContext` transport
  context itself is defined in **Benzene.Core.Messages**, not this package.

### Routing
- `MessageRouter<TContext>` - Routes messages to handlers
- `MessageRouterBuilder` - Builds message routing
- `HandlerPipelineBuilder` - Builds handler pipelines
- `VersionSelector` - Selects handler versions
- `PresetTopicHolder` - scoped (one instance per message), carries the current message's preset
  `ITopic`, or `null` if none was set. **Not on the context** - a context type describes a
  transport message's shape; it shouldn't accumulate optional, cross-cutting routing overrides
  that only some pipelines opt into. This is the concrete example of the "scoped DI state, not
  context" pattern documented in `Benzene.Abstractions.Middleware/CLAUDE.md` - follow it for any
  future per-pipeline override, not context mutation
- `PresetTopicMessageTopicGetter<TContext>` - Decorates a transport's real
  `IMessageTopicGetter<TContext>`, preferring `PresetTopicHolder.PresetTopic` (resolved from the
  same DI scope) when set and falling back to the transport's own extraction otherwise. Every
  transport that supports preset topics (`Benzene.Aws.Lambda.Sqs`, `Benzene.Aws.Sqs`,
  `Benzene.Azure.Function.ServiceBus`) registers this wrapping its real getter as the default
  `IMessageTopicGetter<TContext>`, and registers `PresetTopicHolder` itself
  (`services.TryAddScoped<PresetTopicHolder>()`), so a pipeline that never opts in behaves exactly
  as before this type existed
- `PresetTopicMiddleware<TContext>` - Resolves the current message's `PresetTopicHolder` and sets
  its `PresetTopic` before calling `next`, so `PresetTopicMessageTopicGetter<TContext>` (reading
  the same holder from the same scope) picks it up. Added via `UsePresetTopic<TContext>()`. Not
  generic-constrained on `TContext` at all - it never touches the context
- `MiddlewarePipelineExtensions.UsePresetTopic<TContext>(topicId, version)` - Pipeline-builder
  extension for one queue/subscription: call it before `UseMessageHandlers()` so every message on
  that pipeline routes to `topicId` regardless of what (if anything) the transport message itself
  carries. Solves the case where a queue's producer isn't a Benzene client and never sets a topic
  attribute/property at all - see each transport's own docs for the attribute/property name it
  otherwise reads. Works for ANY `TContext`, with zero changes required to that context type -
  a transport adopting this only needs the two DI-registration lines above

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
