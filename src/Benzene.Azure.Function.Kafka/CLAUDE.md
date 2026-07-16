# Benzene.Azure.Function.Kafka

## What this package does
Azure Kafka integration for Benzene (Azure Event Hubs with Kafka protocol). Provides utilities for working with Event Hubs using Kafka API, enabling Kafka-compatible event processing on Azure.

## Key types/interfaces

### Azure Kafka Integration
- Kafka consumer for Event Hubs
- Kafka producer for Event Hubs
- Event Hubs Kafka configuration

## When to use this package
- When using Event Hubs with Kafka protocol
- For Kafka-compatible applications on Azure
- When migrating Kafka apps to Azure
- For Event Hubs with existing Kafka clients

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Azure.Function.Core** - Azure core utilities
- **Benzene.Kafka.Core** - Kafka core abstractions

## Important conventions
- Uses Event Hubs Kafka endpoint
- Compatible with standard Kafka protocol
- Leverages Event Hubs features via Kafka API
- Connection string format differs from native Event Hubs

## Tests
- `test/Benzene.Core.Test/Azure/KafkaPipelineTest.cs` - full pipeline happy path (real
  `KafkaRecord` through `.UseKafka().UseMessageHandlers()`).
- `test/Benzene.Core.Test/Azure/KafkaGettersTest.cs` - `KafkaMessageBodyGetter` (UTF-8 decode, and
  the null-`Value` → null-body case the pipeline test's happy path doesn't reach),
  `KafkaMessageTopicGetter`, and `KafkaMessageHeadersGetter`'s always-empty contract.
  `KafkaMessageMessageHandlerResultSetter` is a no-op by design (see above) - nothing to assert
  beyond it compiling as a `DefaultMessageMessageHandlerResultSetterBase<KafkaContext>`.
