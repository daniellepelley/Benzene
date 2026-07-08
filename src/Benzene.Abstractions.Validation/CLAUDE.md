# Benzene.Abstractions.Validation

## What this package does
Defines validation abstractions for Benzene. Provides interfaces for request validation that can be implemented by various validation libraries (FluentValidation, DataAnnotations, etc.). Enables validation middleware in the pipeline.

## Key types/interfaces

### Validation Abstractions
- `IValidator<T>` - Validates objects of type T
- `IValidationResult` - Result of validation with errors
- `IValidationFailure` - Individual validation failure

## When to use this package
- When implementing custom validation providers
- As a dependency for validation middleware
- When building validation infrastructure
- Rarely used directly - use FluentValidation or DataAnnotations packages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- Validator interface is generic over request type
- Validation results contain collection of failures
- Failures include property name and error message
- Validation happens before message handler execution
- Multiple validators can be composed
