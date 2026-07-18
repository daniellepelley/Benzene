# Benzene.Xml

## What this package does
Adds XML (`application/xml`) as a negotiable request/response media format for Benzene, backed by
the BCL `System.Xml.Serialization.XmlSerializer`. It plugs an `IMediaFormat<TContext>` into the
content-negotiation pipeline, so a request with `content-type: application/xml` is read as XML and a
response is written as XML when `application/xml` appears in the `accept` header - alongside JSON and
any other registered format, not replacing them.

## Key types
- `XmlSerializer : ISerializer` - wraps `System.Xml.Serialization.XmlSerializer`, caching one
  serializer instance per CLR type in a `ConcurrentDictionary` to avoid the per-call lookup/lock
  overhead. String-based `ISerializer` (produces/consumes XML text); it is **not** an
  `IPayloadSerializer` (no byte-oriented path - contrast `Benzene.Avro`/`Benzene.MessagePack`).
- `XmlMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>` - `ContentType` =
  `Constants.XmlContentType` (`application/xml`); selected by `content-type` on read and `accept` on
  write. `GetSerializer(...)` returns the shared `XmlSerializer`.
- `Constants` - `XmlContentType` (`application/xml`), `ContentTypeHeader` (`content-type`).
- `DependencyInjectionExtensions` - `AddXml()` (open-generic `IMediaFormat<>` for every context),
  `AddXml<TContext>()` (one context), and `UseXml<TContext>()` (pipeline-builder convenience). All
  register the shared `XmlSerializer` via `TryAddSingleton`.

## When to use this package
- When integrating with XML-based or SOAP-like APIs, or legacy systems that speak XML.
- When you need XML available for content negotiation alongside JSON rather than as the sole format.

## Dependencies on other Benzene packages
Direct project references: **Benzene.Abstractions.MessageHandlers** (`ISerializer`,
`IMediaFormat<TContext>`, DI seams), **Benzene.Core.MessageHandlers**
(`AcceptHeaderMediaFormatBase<TContext>`, the content-negotiation base), and
**Benzene.Core.Messages**. `IMiddlewarePipelineBuilder<TContext>` (used by `UseXml`) comes in
transitively via `Benzene.Abstractions.Middleware`. `System.Xml` is part of the BCL - no NuGet
package reference.

## Important conventions
- Registered as an `IMediaFormat<TContext>`, so XML is negotiated per message via `content-type`/
  `accept`; it does not replace the process default serializer.
- Request/response types are serialized by `System.Xml.Serialization.XmlSerializer`'s rules - decorate
  them with `System.Xml.Serialization` attributes as needed (public parameterless ctor required, etc.).
- Serializers are cached per type and shared as a singleton; the format is stateless.
