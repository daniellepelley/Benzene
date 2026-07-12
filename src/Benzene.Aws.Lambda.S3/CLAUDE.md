# Benzene.Aws.Lambda.EventBridge

## What this package does
AWS EventBridge Lambda integration for Benzene. Processes EventBridge events from Lambda triggers through Benzene's message handler pipeline. Handles CloudWatch Events, EventBridge rules, and custom events.

## Key types/interfaces

### Application & Handler
- `EventBridgeApplication` - EventBridge application for Lambda
- `EventBridgeLambdaHandler` - Lambda function handler for EventBridge

### Context
- `EventBridgeContext` - Context for EventBridge event processing

### Message Handling
- `EventBridgeMessageBodyGetter` - Extracts detail from EventBridge event
- `EventBridgeMessageTopicGetter` - Extracts detail-type as topic
- `EventBridgeMessageMessageHandlerResultSetter` - Sets result on context

### Other
- `EventBridgeRegistrations` - Registers EventBridge services
- Extension methods for configuration

## When to use this package
- When building Lambda functions triggered by EventBridge
- For scheduled tasks via EventBridge rules
- When you need event-driven workflows with EventBridge
- For cross-account event processing

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.CloudWatchEvents** - EventBridge event types

## Important conventions
- Detail-type used as message topic
- Event detail contains message payload
- Source identifies event origin
- Resources array available in context
- Scheduled events have different structure
- No response expected - fire-and-forget
