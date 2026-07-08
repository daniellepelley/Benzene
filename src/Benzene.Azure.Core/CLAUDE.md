# Benzene.Azure.Core

## What this package does
Core Azure utilities and abstractions for Benzene. Provides shared Azure functionality used across multiple Azure transport adapters. Foundation for Azure-specific features and service abstractions.

## Key types/interfaces

### Azure Abstractions
- Common Azure context utilities
- Azure service client abstractions
- Shared Azure configuration

## When to use this package
- When building custom Azure integrations with Benzene
- As a dependency for Azure transport adapters
- Rarely used directly - typically transitive via specific Azure packages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Azure SDK** - Various Azure service SDKs

## Important conventions
- Provides Azure-specific abstractions over SDK
- Enables testability of Azure service calls
- Shared configuration for Azure services
