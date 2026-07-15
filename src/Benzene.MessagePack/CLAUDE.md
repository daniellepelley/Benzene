# Benzene.MessagePack

## What this package does
MessagePack serialization integration for Benzene. Provides an `IMediaFormat<TContext>`
(`MessagePackMediaFormat`) backed by MessagePack-CSharp, enabling MessagePack request/response
handling alongside JSON/XML - MessagePack's compact binary encoding is popular in finance and
other high-throughput domains where JSON's text overhead matters.

## Key types/interfaces
- `MessagePackSerializer : IPayloadSerializer` - wraps `global::MessagePack.MessagePackSerializer`.
  MessagePack is genuinely binary, but every Benzene transport's request/response body is a
  `string` today. Rather than throwing `NotSupportedException` from the string-based `ISerializer`
  members (an option `IPayloadSerializer` documents for binary-only formats), this **Base64-armors**
  the msgpack bytes: `Serialize`/`Deserialize` produce/consume Base64 text, so it works unchanged
  through every existing transport's string pipeline. The byte-oriented `IPayloadSerializer`
  members delegate through the same Base64 representation - they stay consistent with the string
  path and still exercise the byte-oriented request-mapping path wherever an
  `IMessageBodyBytesGetter<TContext>` is registered (skipping one intermediate string allocation
  there), but this is **not** a zero-copy raw-binary path.
- `MessagePackMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>` - selected via
  `content-type`/`accept: application/msgpack`, same negotiation mechanics as `Benzene.Xml`'s
  `XmlMediaFormat<TContext>`.
- `Constants.MessagePackContentType` - `"application/msgpack"` (the IANA-registered MIME type).

## When to use this package
- When you need a compact binary wire format alongside or instead of JSON/XML.
- High-throughput / bandwidth-sensitive scenarios (e.g. finance) where MessagePack's size wins
  over JSON matter, and the Base64 overhead of carrying it through a string-bodied transport is
  still worth it.

## Dependencies on other Benzene packages
- **Benzene.Abstractions.MessageHandlers** - `IMediaFormat<TContext>`
- **Benzene.Core.MessageHandlers** - `AcceptHeaderMediaFormatBase<TContext>` (content negotiation)
- **Benzene.Core.Messages** - shared message helpers
- **MessagePack** (MessagePack-CSharp, NuGet) - the underlying binary serializer

## Important conventions
- Register via `AddMessagePack()` (open generic, all contexts) / `AddMessagePack<TContext>()`
  (one context) / `UseMessagePack<TContext>()` (pipeline-builder convenience) - same shape as
  `Benzene.Xml`'s `AddXml()`/`AddXml<TContext>()`/`UseXml<TContext>()`.
- Wire bodies are Base64 text of the msgpack encoding, not raw bytes - a client must Base64-decode
  before msgpack-decoding, and Base64-encode msgpack bytes before sending.
- Can be used alongside JSON/XML/other formats; `IMediaFormatNegotiator<TContext>` picks between
  them per message via `content-type`/`accept`.
