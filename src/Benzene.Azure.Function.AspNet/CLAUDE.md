# Benzene.Azure.Function.AspNet

## What this package does
Azure-specific ASP.NET integration for Benzene. Provides middleware and utilities for running Benzene applications in Azure App Service, Azure Container Apps, and other Azure ASP.NET hosting environments.

## Key types/interfaces

### Azure ASP.NET Integration
- Azure App Service configuration
- Azure-specific middleware
- Azure authentication integration

## When to use this package
- When hosting Benzene apps in Azure App Service
- For Azure Container Apps with ASP.NET Core
- When you need Azure-specific ASP.NET features
- Builds on top of Benzene.AspNet.Core

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.AspNet.Core** - ASP.NET Core integration
- **Benzene.Azure.Function.Core** - Azure core utilities
- **Microsoft.AspNetCore** - ASP.NET Core

## Important conventions
- Extends Benzene.AspNet.Core with Azure features
- Integrates with Azure App Service settings
- Supports Azure authentication
- Azure-specific logging and monitoring
