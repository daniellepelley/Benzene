# Benzene.Grpc

## What this package does
gRPC integration for Benzene. Enables building gRPC services using Benzene's message handler infrastructure, mapping gRPC requests to message handlers and supporting bi-directional streaming.

## Key types/interfaces

### gRPC Integration
- gRPC service implementation using Benzene
- Request/response mapping
- Streaming support
- gRPC context adapter

## When to use this package
- When building gRPC services with Benzene
- For microservices using gRPC
- When you want Benzene features in gRPC
- For high-performance RPC communication

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Grpc.AspNetCore** - gRPC for ASP.NET Core

## Important conventions
- Proto file defines contracts
- Message handlers implement service methods
- Middleware works with gRPC requests
- Supports unary, server streaming, client streaming, bidirectional
- Metadata mapped to Benzene headers
