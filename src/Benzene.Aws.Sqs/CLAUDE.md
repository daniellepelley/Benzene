# Benzene.Aws.Sqs

## What this package does
AWS SQS client utilities for Benzene. Provides abstractions and implementations for publishing messages to SQS queues, handling message attributes, and integrating SQS with Benzene's message infrastructure.

## Key types/interfaces

### SQS Client
- SQS message publisher
- Queue URL resolution
- Message attribute handling

## When to use this package
- When publishing messages to SQS from Benzene apps
- For command/event dispatching via SQS
- When building distributed systems with SQS
- Complements Aws.Lambda.Sqs for receiving messages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Aws.Core** - AWS core utilities
- **AWSSDK.SQS** - AWS SQS SDK

## Important conventions
- Message attributes mapped from Benzene headers
- Queue URLs resolved from configuration
- Batch sending supported for efficiency
- Message deduplication supported
- FIFO queue support included
