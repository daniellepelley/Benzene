# Benzene.Core.Messages

## What this package does
Concrete implementations of Benzene message abstractions. Provides BenzeneMessage format - a transport-agnostic message structure with headers, body, and metadata for use across any transport (HTTP, Kafka, SQS, etc.).

## Key types/interfaces

### BenzeneMessage
- `BenzeneMessage` - Transport-agnostic message
- Message builder
- Header collection
- Message metadata

## When to use this package
- When using BenzeneMessage format
- For transport-agnostic messaging
- When you want consistent message structure
- Used internally by transport adapters

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Messages** - Message abstractions

## Important conventions
- Headers dictionary for metadata
- Body as string or bytes
- Topic/message name in headers
- Correlation ID support
- Works with any transport adapter
