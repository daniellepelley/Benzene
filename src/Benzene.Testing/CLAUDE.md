# Benzene.Testing

## What this package does
Testing utilities and helpers for Benzene applications. Provides test host implementations, builder patterns for test setup, assertion helpers, and utilities for integration testing of Benzene pipelines and handlers.

## Key types/interfaces

### Test Infrastructure
- `BenzeneTestHost` - Test host for integration testing
- Test builders for HTTP and messages
- Test middleware and mocks
- Assertion helpers

## When to use this package
- When writing integration tests for Benzene apps
- For testing message handlers
- When testing middleware pipelines
- For end-to-end testing without external dependencies

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Core.MessageHandlers** - Message handler infrastructure

## Important conventions
- Test host runs full pipeline
- Builders provide fluent test setup
- No external dependencies needed
- Suitable for unit and integration tests
- Can test HTTP and message-based scenarios
- Mock dependencies via DI
