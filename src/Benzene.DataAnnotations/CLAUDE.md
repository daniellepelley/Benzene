# Benzene.DataAnnotations

## What this package does
DataAnnotations validation integration for Benzene. Provides middleware for validating requests using .NET DataAnnotations attributes ([Required], [Range], etc.). Alternative to FluentValidation for simpler validation scenarios.

## Key types/interfaces

### DataAnnotations Integration
- DataAnnotations adapter implementing `IValidator<T>`
- Validation middleware
- Attribute-based validation
- Error response builders

## When to use this package
- When you prefer attribute-based validation
- For simple validation scenarios
- When migrating from ASP.NET MVC/Web API
- Alternative to FluentValidation

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Validation** - Validation abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **System.ComponentModel.DataAnnotations** - .NET DataAnnotations

## Important conventions
- Validation attributes on request properties
- Validation middleware added to pipeline
- Failed validation returns 400 Bad Request
- Standard .NET validation attributes supported
- Custom validation attributes supported
