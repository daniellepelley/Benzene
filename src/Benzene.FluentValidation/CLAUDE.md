# Benzene.FluentValidation

## What this package does
FluentValidation integration for Benzene. Provides middleware and adapters for validating requests using FluentValidation library. Automatically discovers validators from DI container and executes them before message handlers.

## Key types/interfaces

### FluentValidation Integration
- FluentValidation adapter implementing `IValidator<T>`
- Validation middleware
- Automatic validator discovery from DI
- Error response builders

## When to use this package
- When you want to validate requests with FluentValidation
- For complex validation rules
- When you need reusable validation logic
- Standard choice for validation in Benzene apps

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Validation** - Validation abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **FluentValidation** - FluentValidation library

## Important conventions
- Register validators in DI container
- Validation middleware added to pipeline
- Failed validation returns 400 Bad Request
- Validation errors mapped to response
- Validators discovered by type convention
- Async validation supported
