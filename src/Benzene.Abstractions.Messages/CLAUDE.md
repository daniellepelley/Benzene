# Benzene.Abstractions.Messages

## What this package does
Transport-agnostic message abstractions: how a message is addressed (topic), how it is defined
(request type ↔ topic), how it is sent, and the getter/setter "mapper" seam that lets middleware read
and write a transport's body/headers without knowing the concrete transport type. This is the
vocabulary the outbound (`Benzene.Clients`) and inbound message-handler pipelines share.

## Key types/interfaces

### Addressing & definitions
- `ITopic` - a message address: `Id` + `Version`.
- `IMessageDefinition` - binds a `RequestType` to its `ITopic`; `IRequestResponseMessageDefinition`
  adds the response type.
- `IMessageDefinitionFinder` / `IMessageSendersFinder` - lookup of definitions / senders.

### Sending
- `IMessageSender<TRequest>` - `SendMessageAsync(TRequest) → Task<IBenzeneResult>` (fire, no typed
  response).
- `IMessageSender<TRequest, TResponse>` - `SendMessageAsync(TRequest) → Task<IBenzeneResult<TResponse>>`.
- `IMessageSenderBuilder` / `IMessageSenderDefinition` - registration/construction of senders.

### Raw message payloads
- `IRawStringMessage` - a payload carrying pre-rendered string `Content` (bypasses format
  negotiation).
- `IRawContentMessage : IRawStringMessage` - adds `ContentType`, so a handler can deliver
  pre-rendered HTML/etc. with an explicit content type regardless of negotiated format.

### Mappers (the transport-read/write seam), namespace `...Messages.Mappers`
- `IMessageBodyGetter<TContext>` (`GetBody`) / `IMessageBodyBytesGetter<TContext>` /
  `IMessageBodySetter<TContext>`
- `IMessageHeadersGetter<TContext>` (`GetHeaders` → `IDictionary<string,string>`) /
  `IMessageHeadersSetter<TContext>`

### BenzeneClient context, namespace `...Messages.BenzeneClient`
- `IBenzeneClientContext` / `BenzeneClientContext` - the outbound-client invocation context.
- `IBenzeneClientRequest`, `IGetTopic`, `IBenzeneClientContextMiddlewareBuilder` - outbound request +
  topic resolution + the outbound middleware builder seam.

## When to use this package
- When implementing a transport adapter (implement the mapper getters/setters for your context type).
- When building outbound message-sending on top of `IMessageSender<...>`.
- Rarely referenced directly by application code.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - for `IBenzeneResult` / `IBenzeneResult<T>`.

## Important conventions
- Contexts stay transport-shaped; middleware reaches body/headers only through the mapper interfaces,
  never by casting to a concrete transport type.
- `ITopic` carries a version — addressing is `(Id, Version)`, aligning with `Benzene.Core.Versioning`.

## Tests
Interfaces only; exercised through the message-handler and client tests in `test/`.
