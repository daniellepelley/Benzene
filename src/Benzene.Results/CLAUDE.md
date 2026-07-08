# Benzene.Results

## What this package does
Rich result types for Benzene message handlers. Provides Result<T> pattern for explicit success/failure handling, eliminating exceptions for flow control and enabling railway-oriented programming.

## Key types/interfaces

### Result Types
- `Result<T>` - Success or failure result with value
- `Result` - Success or failure result without value
- Extension methods for result composition
- Result pattern helpers

## When to use this package
- When you want explicit error handling
- For railway-oriented programming
- When avoiding exceptions for flow control
- For functional-style error handling

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- Return Result<T> from handlers instead of throwing
- Use extension methods for composition
- Map results through pipeline
- Explicit success/failure semantics
- Status codes attached to results
