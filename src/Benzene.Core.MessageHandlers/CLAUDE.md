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
- `DefaultStatuses` - Standard HTTP/message status codes

### Handler Discovery
- `ReflectionMessageHandlersFinder` - Discovers handlers via reflection
- `DependencyMessageHandlersFinder` - Discovers handlers from DI container
- `CacheMessageHandlersFinder` - Caches discovered handlers
- `CompositeMessageHandlersFinder` - Combines multiple finders
- `MessageHandlersList` - List of discovered handlers
- `MessageHandlerDefinition` - Metadata about a handler
- `MessageHandlerDefinitionLookUp` - Looks up handlers by topic
- `MessageHandlerCandidateTypes` - records the candidate types of one reflection-scanning
  `AddMessageHandlers(Type[])` call (registered cumulatively, one instance per call) so
  cross-cutting diagnostics can see the types discovery *skipped* — consumed by `Benzene.Http`'s
  `UnroutedHttpEndpointCheck` to flag `[HttpEndpoint]` handlers missing their `[Message]`

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
- `DeferredRequestMapper<TContext>` - Deferred request mapping

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
  `IRawContentMessage.ContentType` when the handler's payload implements it. When the payload is an
  `IRawBytesMessage` (raw binary — `Benzene.Core.Messages.RawBytesMessage`), it instead writes the
  bytes verbatim via the byte `SetBody(TContext, ReadOnlyMemory<byte>)` overload and skips
  serialization/negotiation entirely — the response transport handles the encoding (API Gateway
  base64 + `IsBase64Encoded`, self-host raw bytes). Covered by `RawBytesResponseRenderingTest`.
- `ResponseHandlerContainer<TContext>` - Contains response handlers
- `DefaultResponsePayloadMapper<TContext>` - Maps response payloads
- `ResponseIfHandledMessageHandlerResultSetter<TContext>` - Sets result if handled
- `ResponseMessageHandlerResultSetterBase<TContext>` - Base for result setters
- `DefaultMessageHandlerResultSetterBase<TContext>` - Default result setter base
- `MessageHandlerResultSetterBase<TContext>` - shared result-setter base
- `MessageResultRecorder` (`Response/`) - static helper the response-writing setters
  (`ResponseMessageHandlerResultSetterBase`, `ResponseIfHandledMessageHandlerResultSetter`,
  `BenzeneMessageHandlerResultSetter`) call after writing the response to also record the outcome onto
  the context when it implements `IHasMessageResult` (only if not already set). Event-style transports
  report their outcome by *setting* `MessageResult`; request/response transports report by *writing a
  response* and used to leave `MessageResult` unset — which made `UseBenzeneMetrics` tag those
  messages `result=<missing>` (and, downstream, the mesh usage feed show a `<missing>` status). The
  HTTP/API-Gateway/BenzeneMessage contexts now implement `IHasMessageResult` and their setters record
  through this helper, so the same success/failure signal exists on every transport. Guarded by
  `test/Benzene.Core.Test/Core/Core/Response/MessageResultRecordingTest.cs`.

### Serialization
- `JsonSerializer` - System.Text.Json implementation of `ISerializer`; also implements
  `IPayloadSerializer` (byte-oriented, via `Utf8JsonWriter`/`Utf8JsonReader`), so it's used on the
  byte path whenever an `IMessageBodyBytesGetter<TContext>` is registered for the context

### BenzeneMessage (Transport-Agnostic)
- `BenzeneMessageApplication` - Application for BenzeneMessage
- `BenzeneMessageGetter` - reads topic/headers/body off a `BenzeneMessageContext` and implements
  `IMessageBodyBytesGetter` (declared in `BenzeneBodyMapper.cs`, but the class name is
  `BenzeneMessageGetter`)
- `BenzeneMessageHandlerResultSetter` - Sets BenzeneMessage results (writes status + serialized
  body via the response handlers), unless the response is suppressed for this message (below)
- `BenzeneMessageResponseAdapter` - Adapts responses to BenzeneMessage
- `DefaultResponseStatusHandler<TContext>` - Handles response status
- `BenzeneMessageResponseSuppression` / `SuppressBenzeneMessageResponseMiddleware` /
  `SuppressResponse()` - scoped-DI-holder (like `PresetTopicHolder`) that tells
  `BenzeneMessageHandlerResultSetter` to skip response serialization on a one-way host (Event Hub,
  Queue Storage) that discards it. Defaults off; those hosts' `UseBenzeneMessage(action)` add it
  automatically, request/response hosts don't

### Context
- `MessageHandlerContext<TRequest, TResponse>` - Generic handler context (the concrete
  `IMessageHandlerContext<TRequest, TResponse>`). Note: the `BenzeneMessageContext` transport
  context itself is defined in **Benzene.Core.Messages**, not this package.

### Routing
- `MessageRouter<TContext>` - Routes messages to handlers
- `MessageRouterBuilder` - Builds message routing
- `HandlerPipelineBuilder` - Builds handler pipelines. The pipeline *structure* (which
  `IHandlerMiddlewareBuilder`s, in what order) is fixed once the builder set is known at startup, so
  `Create` no longer rebuilds the whole chain per message. It resolves a cached, handler-agnostic
  structure from `HandlerPipelineStructureCache<TRequest,TResponse>` (a generic-static cache keyed by
  the **builder-set identity**, per type pair - *not* by `(TRequest,TResponse)` alone, which would let
  two pipelines sharing a handler but registering different middleware cross-contaminate) and wraps it
  with the current message's handler in a `HandlerMiddlewarePipeline<TRequest,TResponse>`. That
  wrapper folds the cached factories into a chain per invocation, resolving each middleware (and the
  terminal handler middleware) from the **per-call** `IServiceResolver` - the same "structure once,
  instances per request" split as `MiddlewarePipeline<TContext>`, so a single cached structure is safe
  to share across concurrent batch records. Non-breaking (no public signature changed);
  middleware-builder construction is now deferred from build time to invocation (matching the
  top-level pipeline, so `UseExceptionHandler` covers construction failures). The builder set is fed
  in once per `UseMessageHandlers` call and hoisted out of the per-message closure (`Extensions.cs`)
  so the same array reference is the stable cache key. Guarded by
  `HandlerPipelineBuilderCachingTest` (key isolation + per-record scoped resolution under concurrency)
  and benchmarked by `HandlerCreationBenchmarks` (per-message build allocation dropped from
  ~408-792 B, scaling with middleware count, to a flat 32 B).
- `VersionSelector` - Selects handler versions
- `HeaderMessageVersionGetter<TContext>` - the default `IMessageVersionGetter<TContext>`: reads the
  payload schema version from the header dictionary, trying each name in an ordered fallback list
  (default `["benzene-version", "version", "x-version"]`, `DefaultHeaderNames`)
- `MessageVersionHeaderNames` + `MessageVersionHeaderNamesExtensions` - the header-name fallback is
  an **application-wide** concern (same set regardless of transport, unlike a per-transport topic
  key), so it's overridden in one place: `AddMessageVersionHeaderNames(...)` registers a
  `MessageVersionHeaderNames` that every transport's getter resolves at message-handle time. Each
  transport registers its version getter via `AddHeaderMessageVersionGetter<TContext>()`
  (`TryAdd...` for `BenzeneMessageContext`); the HTTP transports layer the route-parameter check in
  front via `HttpMessageVersionGetterBase` subclasses that thread the same override. No override
  registered ⇒ `DefaultHeaderNames`
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
- `MessageGetter<TContext>` - Gets messages from context. `GetTopic` **memoizes** via a scoped
  `ResolvedTopicCache<TContext>` (below) so the transport's topic extraction runs once per message,
  not once per consumer - the router, the health-check middleware and every tracing decorator's
  per-subsegment tagging all call it (~a dozen times on a traced Lambda). The cache is an optional
  ctor arg (`null` = extract every call, unchanged - the shape direct-construction tests use); DI
  supplies it. `BenzeneMessageGetter` (the `BenzeneMessageContext` getter) is separate and unmemoized -
  its topic is a plain property read.
- `ResolvedTopicCache<TContext>` - scoped (one per message) cache of the resolved `ITopic`, generic
  on `TContext` so a multi-transport function never serves one transport's topic to another.
  `PresetTopicMiddleware` calls `Reset()` when it applies a preset, so a topic memoized by a
  middleware that ran *before* the preset (e.g. a tracing decorator) isn't served to the router in
  place of the preset - the router re-resolves and sees the preset. Registered by `AddContextItems`.
  Mirrors the memoizing-scoped-holder pattern of `MediaFormatNegotiator`/`PresetTopicHolder`.
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
- JsonSerializer uses System.Text.Json (can be replaced via DI) — **but note which registration
  the negotiated path actually uses**: `JsonMediaFormat<TContext>` binds to the **concrete
  `JsonSerializer`**, not `ISerializer`, so replacing only the `ISerializer` registration does NOT
  change what the default JSON request/response path serializes with. To install custom
  `JsonSerializerOptions` globally (e.g. polymorphism config), register the concrete
  `JsonSerializer` (its ctor takes options) **before** `AddBenzene()` — both registrations are
  `TryAdd`, so the earlier one wins — or plug a custom `IMediaFormat<TContext>` (see
  `Benzene.Xml` for the pattern). Payload models annotated with STJ's
  `[JsonPolymorphic]`/`[JsonDerivedType]` polymorphism attributes work with the default serializer
  as-is (STJ honors model attributes under any options)
- The default `JsonSerializer()` options use `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, **not**
  STJ's default HTML-safe encoder. Benzene writes JSON to API clients/browsers, never HTML, so the
  default encoder's escaping of `<` `>` `&` `'` (to `\uXXXX`) only produces unreadable wire bodies —
  e.g. a framework error `detail` like `No handler found for topic '<missing>'` would otherwise
  serialize into the body as `No handler found for topic \u0027\u003Cmissing\u003E\u0027`. Relaxed
  escaping writes those characters literally. Supplying custom `JsonSerializerOptions` via the other ctor overrides
  this (set your own `Encoder` if you serve Benzene JSON unescaped into an HTML context)
