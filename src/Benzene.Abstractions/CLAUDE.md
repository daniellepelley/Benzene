# Benzene.Abstractions

## What this package does
Core abstraction layer for Benzene. Defines fundamental interfaces for dependency injection, logging, serialization, and result types. All other Benzene packages depend on these abstractions to maintain loose coupling and testability.

## Key types/interfaces

### Dependency Injection
- `IBenzeneServiceContainer` - Container abstraction for registering dependencies
- `IServiceResolver` - Resolves dependencies from the container
- `IServiceResolverFactory` - Factory for creating service resolvers
- `IDependencyInjectionAdapter<T>` - Adapter for integrating third-party DI containers
- `IRegisterDependency` - Marker interface for registration modules
- `BenzeneServiceContainerExtensions` - Extension methods for fluent registration

### Logging
- `IBenzeneLogger` - Logger abstraction (provider-agnostic)
- `IBenzeneLogContext` - Log context with structured data (IDisposable)
- `IBenzeneLogAppender` - Appends structured data to log context
- `ILogContextBuilder<T>` - Fluent builder for creating log contexts
- `BenzeneLogLevel` - Enum: Trace, Debug, Information, Warning, Error, Critical

### Serialization
- `ISerializer` - Abstraction for serializing/deserializing objects

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
- `IDependencyWrapper<T>` - Wraps dependencies for specific contexts

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
- Logging interfaces support structured logging via `IBenzeneLogContext`
- DI abstractions follow standard container patterns (Register, Resolve)
- Extension methods provide fluent registration API
- `Void` class (not struct) represents absence of payload - use for handlers with no response
- All abstractions are designed to be easily testable with mocks/stubs
