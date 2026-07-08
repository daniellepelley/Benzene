# Benzene.Aws.Kafka

## What this package does
AWS Kafka utilities for Benzene (outside Lambda context). Provides types and utilities for working with AWS MSK and Kafka events, shared between Lambda and non-Lambda Kafka integrations.

## Key types/interfaces

### Kafka Utilities
- Kafka event models
- Message transformations
- Topic and partition utilities

## When to use this package
- When working with Kafka outside Lambda
- As a shared dependency for Kafka integrations
- Typically used transitively via Aws.Lambda.Kafka

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Aws.Core** - AWS core utilities

## Important conventions
- Provides Kafka-specific models
- Shared between different Kafka scenarios
- Lower-level than Aws.Lambda.Kafka
