# Benzene.Aws.Core

## What this package does
Core AWS utilities and abstractions for Benzene. Provides shared AWS functionality used across multiple AWS transport adapters. Foundation for AWS-specific features like X-Ray tracing, SNS/SQS clients, and AWS service abstractions.

## Key types/interfaces

### AWS Abstractions
- Common AWS context utilities
- AWS service client abstractions
- Shared AWS configuration

## When to use this package
- When building custom AWS integrations with Benzene
- As a dependency for AWS transport adapters
- Rarely used directly - typically transitive via specific AWS packages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **AWS SDK** - Various AWS service SDKs

## Important conventions
- Provides AWS-specific abstractions over SDK
- Enables testability of AWS service calls
- Shared configuration for AWS services
