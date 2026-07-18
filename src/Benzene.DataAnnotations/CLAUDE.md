# Benzene.DataAnnotations

## What this package does
Validates request objects with the BCL `System.ComponentModel.DataAnnotations` attributes
(`[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`, custom `ValidationAttribute`s, …)
as Benzene pipeline middleware. For each message it runs `Validator.TryValidateObject(...)` on the
request before the handler runs and short-circuits with a `ValidationError` result carrying the
attributes' error messages. A deliberately minimal alternative to `Benzene.FluentValidation` - no DI
scanning, no per-rule status mapping, and no schema-builder integration.

## Key types/interfaces
- `ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>`
  (Name `"DataAnnotationValidation"`) - null request → `BenzeneResult.ValidationError<TResponse>("Request is null")`;
  otherwise runs `Validator.TryValidateObject(request, ctx, results, validateAllProperties: true)` and,
  if any `ValidationResult` was produced, sets `BenzeneResult.ValidationError<TResponse>(errorMessages)`
  and stops the pipeline. No errors → `next()`.
- `ValidationMiddlewareBuilder : IHandlerMiddlewareBuilder` - constructs the middleware per handler
  (no dependencies to resolve; unlike FluentValidation there is no status mapper).
- `DependencyExtensions.UseDataAnnotationsValidation(this IMessageRouterBuilder)` - the single entry
  point; adds `ValidationMiddlewareBuilder` to the pipeline. There is no `AddDataAnnotations` DI step
  and nothing to register, because validation reads attributes off the request type at runtime.

## When to use this package
- When request rules are simple enough to express as attributes and you don't need a schema builder,
  custom result statuses, or async validation.
- When migrating request models that already carry ASP.NET MVC / Web API DataAnnotations attributes.
- Prefer `Benzene.FluentValidation` when you also want validation rules reflected into the OpenAPI/
  `benzene` spec, or per-failure result-status control.

## Dependencies on other Benzene packages
- **Benzene.Abstractions.MessageHandlers** - `IMessageHandlerContext<>`, `IMessageRouterBuilder`,
  `IHandlerMiddlewareBuilder`.
- **Benzene.Abstractions.Middleware** - `IMiddleware<>`.
- **Benzene.Results** - `BenzeneResult` (`ValidationError`).
- `System.ComponentModel.DataAnnotations` is part of the BCL - no NuGet package reference.

## Important conventions
- Put `ValidationAttribute`s on the request type's properties; the middleware validates all
  properties (`validateAllProperties: true`).
- The result is always the `ValidationError` status - this package has no per-rule status mapping.
- Validation is synchronous (`Validator.TryValidateObject`); there is no async validation path.
- A request type with no annotations simply passes (an empty result list), it is not rejected.
