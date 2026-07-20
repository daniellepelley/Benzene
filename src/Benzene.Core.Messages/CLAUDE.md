# Benzene.Core.Messages

## What this package does
Concrete implementations of the message abstractions: the in-process "BenzeneMessage" transport
shape (topic + headers + body), the outbound message-sender implementation, topic/raw-message value
types, and the context-predicate helpers used for pipeline branching. There is no single
`BenzeneMessage` type — the message is modelled as a request/response pair on a context.

## Key types/interfaces

### BenzeneMessage transport shape (`Benzene.Core.Messages.BenzeneMessage`)
- `IBenzeneMessageRequest` / `BenzeneMessageRequest` - inbound message: `Topic`, `Headers`, `Body`
  (string).
- `IBenzeneMessageResponse` / `BenzeneMessageResponse` - outbound: `StatusCode`, `Headers`, `Body`.
- `BenzeneMessageContext` - pairs a request with a mutable response; this is the `TContext` the
  in-process/direct-invoke pipeline flows.

### Addressing & raw payloads
- `Topic : ITopic` - `Id` + `Version` value type (version defaults to empty).
- `RawStringMessage : IRawStringMessage` - wraps pre-rendered string `Content`.
- `RawBytesMessage : IRawBytesMessage` - wraps a raw binary `Content` (`ReadOnlyMemory<byte>`) + its
  `ContentType`, for a handler returning bytes verbatim (image/PDF/zip). `SerializerResponseRenderer`
  writes it through the byte `SetBody` overload; HTTP transports encode as required (API Gateway
  base64 + `IsBase64Encoded`, self-host raw bytes).

### Outbound sending (`Benzene.Core.Messages.MessageSender`)
- `MessageSender<TMessage>` / `MessageSender<TRequest, TResponse>` - `IMessageSender<...>`
  implementations.
- `IMessageSenderBuilder` / `MessageSenderBuilder` / `MessageSenderDefinition` - registration &
  construction of senders.
- `BenzeneClientContext<TRequest, TResponse>` / `BenzeneClientRequest<TMessage>` /
  `DefaultGetTopic` - the outbound client context, request wrapper, and default topic resolver.

### Context predicates (`Benzene.Core.Messages.Predicates`) — pipeline branching
- `ContextPredicateBuilder<TContext>`, `InlineContextPredicate<TContext>`,
  `HeaderContextPredicate<TContext>`, `MediaTypeHeaderContextPredicate<TContext>` - build the
  `IContextPredicate<TContext>` conditions used by `.Split(...)`.

### Helpers
- `MediaType`, `DictionaryUtils`, `Constants`.

## When to use this package
- When using the in-process BenzeneMessage transport (e.g. direct Lambda message invoke).
- When sending messages outbound via `IMessageSender<...>`.
- When building predicate-based pipeline branches.
- Typically a transitive dependency of transport adapters.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions.
- **Benzene.Abstractions.Messages** - Message abstractions this package implements.

## Important conventions
- `BenzeneMessageContext` stays transport-shaped (request in, response out) — no business concerns.
- Topic is `(Id, Version)`; a missing id falls back to a `Constants.Missing.Id` sentinel.
- Headers carry metadata (topic/routing, content type); the body is a string.

## Tests
Covered by `test/Benzene.Core.Test` (message sending, topic resolution, context predicates).
