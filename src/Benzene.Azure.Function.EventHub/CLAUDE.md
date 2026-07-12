# Benzene.Azure.Function.EventHub

## What this package does
Azure Event Hubs integration for Benzene. Provides consumers and producers for Event Hubs, enabling event stream processing through Benzene's message handler pipeline. Supports Azure Functions Event Hub triggers.

## Key types/interfaces

### Event Hub Integration
- Event Hub consumer implementation
- Event Hub producer implementation
- Event Hub context and adapters
- Message handling for Event Hub events

## When to use this package
- When consuming events from Azure Event Hubs
- For event stream processing in Azure
- With Azure Functions Event Hub triggers
- For real-time analytics and event sourcing

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Azure.Function.Core** - Azure core utilities
- **Azure.Messaging.EventHubs** - Event Hubs SDK

## Important conventions
- Supports batch processing of events
- Partition key extracted from context
- Event metadata available in context
- Checkpointing supported
- Consumer groups for scaling
