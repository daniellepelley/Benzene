# Benzene.Clients.Aws

## What this package does
AWS client implementations for calling Benzene services in AWS. Provides clients for Lambda, SQS, SNS, and other AWS services that host Benzene applications.

## Key types/interfaces

### AWS Clients
- Lambda invocation client
- SQS message client
- SNS publish client
- AWS service integration

## When to use this package
- When calling Benzene Lambda functions
- For publishing to Benzene SQS consumers
- For SNS-based service communication
- For AWS-native service calls

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Clients** - Client abstractions
- **Benzene.Aws.Core** - AWS core utilities
- **AWS SDK** - AWS service clients

## Important conventions
- Uses AWS SDK clients
- IAM-based authentication
- Region configuration
- ARN-based addressing
- `SqsContextConverter`/`SnsContextConverter` forward `IBenzeneClientRequest.Headers` onto real
  `MessageAttributes` (alongside the `topic` attribute) so header-based decorators (correlation ID,
  W3C trace context) actually reach the wire
- `LambdaContextConverter` (used by the lower-level `UseAwsLambda()` pipeline composition, not the
  `AwsLambdaBenzeneMessageClient`/`CreateAwsLambdaBenzeneMessageClient()` sugar) does NOT forward
  headers — a raw `InvokeRequest` has no header-like concept. `AwsLambdaBenzeneMessageClient` already
  forwards headers correctly by embedding them in its own `BenzeneMessageClientRequest` envelope.
