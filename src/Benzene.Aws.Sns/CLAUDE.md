# Benzene.Aws.Sns

## What this package does
AWS SNS client utilities for Benzene. Provides abstractions and implementations for publishing messages to SNS topics, handling message attributes, and integrating SNS with Benzene's message infrastructure.

## Key types/interfaces

### SNS Client
- SNS message publisher
- Topic ARN resolution
- Message attribute handling

## When to use this package
- When publishing events to SNS from Benzene apps
- For pub/sub event distribution
- When building event-driven architectures with SNS
- Complements Aws.Lambda.Sns for receiving messages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Aws.Core** - AWS core utilities
- **AWSSDK.SimpleNotificationService** - AWS SNS SDK

## Important conventions
- Message attributes mapped from Benzene headers
- Topic ARNs resolved from configuration
- Message subject supported
- Filtering policies enabled via attributes
- Fan-out pattern for multiple subscribers
