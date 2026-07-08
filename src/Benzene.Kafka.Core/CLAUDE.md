# Benzene.Kafka.Core

## What this package does
Core Kafka integration for Benzene. Provides abstractions and implementations for Kafka consumers and producers, enabling message processing with Apache Kafka. Foundation for AWS and Azure Kafka packages.

## Key types/interfaces

### Kafka Core
- Kafka consumer abstractions
- Kafka producer abstractions
- Kafka context
- Message key/value handling

## When to use this package
- When building Kafka-based applications
- For Kafka consumer/producer implementations
- As foundation for cloud Kafka services
- Used by Aws.Kafka and Azure.Kafka

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Confluent.Kafka** - Kafka client library

## Important conventions
- Consumer groups for scaling
- Partition assignment
- Offset management
- Message key for partitioning
- Headers for metadata
- Commit strategies configurable
