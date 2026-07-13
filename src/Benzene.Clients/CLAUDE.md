# Benzene.Clients

## What this package does
Client abstractions for calling Benzene services. Provides interfaces and base implementations for building type-safe clients that communicate with Benzene HTTP endpoints and message handlers.

## Key types/interfaces

### Client Infrastructure
- Client abstractions
- Request/response mapping for clients
- Client builder patterns
- Type-safe client interfaces
- `ClientBuilder`/`IDependencyWrapper<IBenzeneMessageClient>` - decorator-chain pattern for
  `IBenzeneMessageClient`; existing decorators: `CorrelationId/` (`WithCorrelationId()`),
  `TraceContext/` (`WithW3CTraceContext()` - stamps `Activity.Current`'s `traceparent`/`tracestate`
  onto outgoing headers), `HeaderBenzeneMessageClient`/`HeadersBenzeneMessageClient`,
  `RetryBenzeneMessageClient`

## When to use this package
- When building clients for Benzene services
- For type-safe service communication
- When generating API clients
- Foundation for specific client implementations

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- Type-safe request/response
- Async client methods
- Transport-agnostic interface
- Used by HTTP and AWS clients
