# Benzene.Abstractions

## What this package does
Core abstraction layer for Benzene. Defines fundamental interfaces for dependency injection, logging, serialization, and result types. All other Benzene packages depend on these abstractions to maintain loose coupling and testability.

## Key types/interfaces

### Dependency Injection
- `IBenzeneServiceContainer` - Container abstraction for registering dependencies
- `IServiceResolver` - Resolves dependencies from the container
- `IServiceResolverFactory` - Factory for creating service resolvers
- `IRegisterDependency` - Marker interface for registration modules
- `BenzeneServiceContainerExtensions` - Extension methods for fluent registration

### Logging
Benzene logs through `Microsoft.Extensions.Logging` (`ILogger<T>`/`ILoggerFactory` from
Microsoft.Extensions.Logging.Abstractions, referenced by this package) — there is no
Benzene-specific logger interface. What remains here is the scope-enrichment builder:
- `ILogContextBuilder<T>` - Fluent builder producing log-scope state (fed to `ILogger.BeginScope`)
- `LogContextBuilderExtensions` - `OnRequest`/`OnResponse` convenience overloads

### Serialization
- `ISerializer` - Abstraction for serializing/deserializing objects
- `IPayloadSerializer : ISerializer` - Additive byte-oriented extension (`Serialize(Type, object, IBufferWriter<byte>)` / `Deserialize(Type, ReadOnlySpan<byte>)`), avoiding an intermediate string allocation when both the serializer and the transport's body getter support bytes

### Results
- `IBenzeneResult` - Marker interface for result types
- `IBenzeneResult<T>` - Generic result with typed payload
- `Void` - Unit type for handlers with no response payload

### Builders & Testing
- `IHttpBuilder<T>` - Fluent builder for HTTP-based tests
- `IMessageBuilder<T>` - Fluent builder for message-based tests
- `IBenzeneTestHost` - Test host abstraction

### Other
- `ICorrelationId` - Provides correlation ID for request tracking
- `IDependencyWrapper<T>` - **`[Obsolete]`** (2026-07-17) - wraps dependencies for specific
  contexts; its one real consumer, `Benzene.Clients`'s outbound decorator chain, is being
  superseded by ordinary outbound middleware - see `work/benzene-clients-redesign-plan.md`

## When to use this package
- When implementing new Benzene middleware or extensions
- When creating custom DI adapters (Autofac, StructureMap, etc.)
- When implementing custom logging providers
- When writing unit tests that depend on Benzene abstractions
- This is the foundation package - rarely used directly by application code

## Dependencies on other Benzene packages
None - this is the root abstraction layer with no Benzene dependencies.

## Important conventions
- All interfaces use the `I` prefix
- Structured logging uses `ILogger.BeginScope`; `ILogContextBuilder<T>` builds the scope state
- DI abstractions follow standard container patterns (Register, Resolve)
- Extension methods provide fluent registration API
- `Void` class (not struct) represents absence of payload - use for handlers with no response
- All abstractions are designed to be easily testable with mocks/stubs
