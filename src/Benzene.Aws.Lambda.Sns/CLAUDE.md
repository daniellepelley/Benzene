# Benzene.Aws.Lambda.Sns

## What this package does
AWS SNS Lambda integration for Benzene. Processes SNS events from Lambda triggers through Benzene's message handler pipeline. Handles SNS message structure, attributes, and notification processing.

## Key types/interfaces

### Application & Handler
- `SnsApplication` - SNS application for Lambda
- `SnsLambdaHandler` - Lambda function handler for SNS

### Context
- `SnsContext` - Context for SNS message processing

### Message Handling
- `SnsMessageBodyGetter` - Extracts message body from SNS event
- `SnsMessageHeadersGetter` - Extracts message attributes as headers
- `SnsMessageTopicGetter` - Extracts topic from SNS topic ARN
- `SnsMessageMessageHandlerResultSetter` - Sets result on context

### Other
- `SnsRegistrations` - Registers SNS services
- Extension methods for configuration

## When to use this package
- When building Lambda functions triggered by SNS
- For pub/sub event handling with Benzene
- When you need fan-out message delivery
- For event-driven architectures using SNS

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.SNSEvents** - SNS event types

## Important conventions
- Processes SNS notifications in batches
- Message attributes mapped to headers
- Topic extracted from SNS topic ARN
- Message subject available in context
- SNS wraps message body - unwrapped automatically
- Subscription confirmation handled separately
- No response expected - fire-and-forget pattern
