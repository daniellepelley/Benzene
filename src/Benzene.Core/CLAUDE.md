# Benzene.Core

## What this package does
Provides concrete implementations of core Benzene abstractions. Includes default implementations for logging, DI registration tracking, exception types, and utility classes. This is the foundation implementation package that other Core.* packages build upon.

## Key types/interfaces

### Logging Implementations
Logging goes through `Microsoft.Extensions.Logging` (`ILogger<T>`); this package only provides
the scope-state builders used by `UseLogResult`/`UseLogContext`:
- `LogContextBuilder<TContext>` - Builds log-scope state from typed context (implements `ILogContextBuilder<TContext>`)
- `ContextDictionaryBuilder<TContext>` - Builds dictionary from context
- `IContextDictionaryBuilder<TContext>` - Abstraction for context dictionary building

### DI Registration
- `IRegistrations` - Interface for registration modules
- `IRegistrationCheck` - Validates registrations are complete
- `RegistrationCheck` - Default registration diagnostics. Given a **missing-registration failure** it
  produces the actionable "you might be missing `.UsingBenzene(x => x.AddXxx())`" hint. Three entry
  points, in increasing robustness:
  - `CheckType(typeName)` - guidance for a specific type name (used by the resolver adapters, keyed on
    `typeof(T)`, so it's **container-independent** - it never parses a container's exception text).
  - `CheckException(Exception)` - best-effort scan of a container's resolve exception (and its inner
    exceptions) for any known type name. Deliberately **wording-agnostic** (quoted-token first, then
    namespaced whitespace tokens) so it works across Microsoft DI, Autofac, and third-party containers,
    across framework versions, and across cultures - not tied to one container's message prefix. All
    parsing is bounds-safe and it **never throws**, so a diagnostic can't replace the real error.
  - `Describe(requestedType, Exception)` - what the adapters call: prefer `CheckType(requestedType)`
    (reliable everywhere) and fall back to `CheckException` for a missing *transitive* dependency the
    container names. Also never throws.
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
- `ContextDictionaryBuilder` enables extracting log-scope properties from any object
- Registration validation helps ensure DI container is properly configured
- Constants class provides shared magic strings across framework
- All implementations are thread-safe where applicable
