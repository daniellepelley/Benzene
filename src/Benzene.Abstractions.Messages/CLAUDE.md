# Benzene.Abstractions.Messages

## What this package does
Defines message abstractions for Benzene. Provides interfaces for message structure, metadata, and transport-agnostic message handling. Foundation for BenzeneMessage format.

## Key types/interfaces

### Message Abstractions
- Message interface definitions
- Message metadata abstractions
- Header and body abstractions

## When to use this package
- When implementing custom message formats
- When building transport adapters
- Rarely used directly - consumed by Core.Messages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- Messages have headers and body
- Metadata separate from payload
- Transport-agnostic design
- Used by BenzeneMessage implementation
