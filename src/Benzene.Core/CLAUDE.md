# Benzene.Core

## What this package does
Provides concrete implementations of core Benzene abstractions. Includes default implementations for logging, DI registration tracking, exception types, and utility classes. This is the foundation implementation package that other Core.* packages build upon.

## Key types/interfaces

### Logging Implementations
- `BenzeneLogger` - Default logger implementation wrapping `IBenzeneLogger`
- `NullBenzeneLogger` - Null object pattern logger (no-op)
- `NullBenzeneLogContext` - Null object pattern log context
- `NullDisposable` - Null object pattern for IDisposable
- `LogContextBuilder<TContext>` - Builds log context from typed context
- `ContextDictionaryBuilder<TContext>` - Builds dictionary from context
- `IContextDictionaryBuilder<TContext>` - Abstraction for context dictionary building

### DI Registration
- `IRegistrations` - Interface for registration modules
- `IRegistrationCheck` - Validates registrations are complete
- `RegistrationCheck` - Default registration validation
- `RegistrationsBase` - Base class for registration modules
- `RegistrationRecorder` - Records registration operations for validation
- `RegistrationMatch` - Matches registered types

### Exceptions
- `BenzeneException` - Base exception type for Benzene framework

### Utilities
- `DictionaryUtils` - Dictionary manipulation helpers
- `Utils` - General utility methods
- `Constants` - Framework constants

### Extension Methods
- `LogContextExtensions` - Extensions for `IBenzeneLogContext`
- `LoggerExtensions` - Extensions for `IBenzeneLogger`
- Various DI-related extensions

## When to use this package
- When you need default implementations of Benzene abstractions
- When building on top of Benzene Core functionality
- When creating custom middleware or message handlers
- This package is typically a transitive dependency via Core.Middleware or Core.MessageHandlers

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Implements interfaces from this package

## Important conventions
- Null object pattern used extensively for optional components
- `RegistrationsBase` is the standard base class for DI registration modules
- Logger extensions support structured logging with key-value pairs
- `ContextDictionaryBuilder` enables extracting log context from any object
- Registration validation helps ensure DI container is properly configured
- Constants class provides shared magic strings across framework
- All implementations are thread-safe where applicable
