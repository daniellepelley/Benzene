# Data Annotations

`Benzene.DataAnnotations` validates a message handler's request using standard .NET `System.ComponentModel.DataAnnotations` attributes (`[Required]`, `[Range]`, `[MaxLength]`, `[RegularExpression]`, etc.), short-circuiting the pipeline with a `validation-error` result when any attribute fails.

## Overview

`System.ComponentModel.DataAnnotations` is the .NET namespace of built-in validation attributes you may already be using for ASP.NET Core model binding or Entity Framework Core. `Benzene.DataAnnotations` runs those same attributes against a message handler's request type inside the middleware pipeline, via `ValidationMiddleware<TRequest, TResponse>`, added with `.UseDataAnnotationsValidation()` inside `.UseMessageHandlers(...)`.

Unlike `Benzene.FluentValidation`, this middleware requires no separate validator class and no DI registration — it always runs `System.ComponentModel.DataAnnotations.Validator.TryValidateObject(...)` against the request object, using whatever attributes are already declared on its properties. For each request:

- If the request is `null`, the pipeline short-circuits with a `validation-error` result (`"Request is null"`).
- The middleware validates every property of the request via `Validator.TryValidateObject(request, context, results, validateAllProperties: true)`.
- If any attribute fails, the pipeline short-circuits with a `validation-error` result containing the collected error messages — the status is always `validation-error`; there is no per-rule or per-handler override (see [Comparison with FluentValidation](#comparison-with-fluentvalidation)).
- If every attribute passes (or the request type has no validation attributes at all), `next()` is called and the handler runs as normal.

Use this package when your validation needs are simple attribute checks, when you're migrating request models from ASP.NET MVC/Web API that already carry `DataAnnotations` attributes, or when you want validation with zero extra registration.

## Installation

```bash
dotnet add package Benzene.DataAnnotations
```

This package has no third-party NuGet dependency — it only uses the `System.ComponentModel.DataAnnotations` types built into .NET.

## Basic Usage

Add validation attributes to your request type:

```csharp
using System.ComponentModel.DataAnnotations;

public class CreateWidgetRequest
{
    [Required]
    [MaxLength(10)]
    public string Name { get; set; }
}
```

Wire the middleware into the message handler pipeline:

```csharp
using Benzene.DataAnnotations;

app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseMessageHandlers(router => router
        .UseDataAnnotationsValidation()
    )
);
```

No further registration is required — `.UseDataAnnotationsValidation()` only adds the middleware to the handler pipeline; it does not need to discover anything from DI, because the validation rules live directly on the request type as attributes.

If `Name` is missing or longer than 10 characters, the handler is never invoked and the caller receives a `validation-error` result with messages such as `"The Name field is required."`.

## Configuration

There is nothing to configure beyond adding the middleware to the pipeline — `.UseDataAnnotationsValidation()` takes no parameters, registers no services, and there is no assembly/type scanning step (contrast with `Benzene.FluentValidation`'s `UseFluentValidation(assemblies)` overloads).

Every property on the request is checked (`validateAllProperties: true`), including attributes like `[Range]` and `[RegularExpression]` that only run when explicitly requested — not just `[Required]`. If a request type has no validation attributes at all, validation trivially succeeds and the request passes through unchanged.

## Composing inside `.UseMessageHandlers()`

`.UseDataAnnotationsValidation()` is a handler-pipeline middleware builder (`IHandlerMiddlewareBuilder`), added inside the `router` passed to `.UseMessageHandlers(...)` — the same place a handler's request/response types have been resolved, before the handler is invoked:

```csharp
app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseTimer("benzene-message")
    .UseLogResult(x => x.WithCorrelationId())
    .UseMessageHandlers(router => router
        .UseDataAnnotationsValidation()
    )
);
```

Because it runs for every request that reaches a handler, adding it once here validates every handler's request type in the same pipeline — there's no per-handler opt-in step.

> `Benzene.FluentValidation` and `Benzene.DataAnnotations` both register `ValidationMiddleware<TRequest, TResponse>` under the pipeline, so avoid adding both `.UseFluentValidation()` and `.UseDataAnnotationsValidation()` to the same router unless you intend for a request to be checked by both mechanisms.

## Comparison with FluentValidation

| | `Benzene.FluentValidation` | `Benzene.DataAnnotations` |
|---|---|---|
| Where rules live | Separate `AbstractValidator<T>` class | Attributes on the request type's properties |
| Opt-in per request type | Yes — only runs if an `IValidator<TRequest>` is registered/discovered for that type | No — always runs `Validator.TryValidateObject` on every request that reaches the middleware |
| DI / registration required | Yes — validators discovered from assemblies/types and registered via `TryAddSingleton` | No — no services are registered |
| Null request | `validation-error`, message `"Request is null"` | `validation-error`, message `"Request is null"` |
| Failure status | Configurable — defaults to `validation-error`, but can be overridden per-rule (`.WithStatus(...)`) or per-handler (`[ValidationStatus(...)]`) via `IValidationStatusMapper` | Always `BenzeneResultStatus.ValidationError` — no override mechanism |
| Schema/OpenAPI generation | Yes — `IValidationSchemaBuilder` (`FluentValidationSchemaBuilder`) reflects over rules | Not provided by this package |
| Complex/cross-property rules | Yes — full fluent rule composition | Limited to what a single `ValidationAttribute` can express (or a custom attribute / `IValidatableObject`) |

If you need to vary the returned status per rule or per handler, or need schema generation for documentation, use `Benzene.FluentValidation` instead. If your request types are simple and already carry (or can easily carry) standard attributes, `Benzene.DataAnnotations` avoids the extra validator classes and registration.

## Custom validation attributes

Because the middleware calls the standard `System.ComponentModel.DataAnnotations.Validator`, any custom attribute deriving from `ValidationAttribute` (or a request type implementing `IValidatableObject`) is honored the same way it would be by ASP.NET Core model validation:

```csharp
using System.ComponentModel.DataAnnotations;

public class PositiveEvenNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is int number && number > 0 && number % 2 == 0)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Value must be a positive, even number.");
    }
}

public class CreateWidgetRequest
{
    [PositiveEvenNumber]
    public int Quantity { get; set; }
}
```

## See Also
- [Fluent Validation](fluent-validation.md) — the rule-based alternative with configurable failure statuses and schema generation
- [Message Handlers](message-handlers.md) — where validation middleware sits in the request lifecycle
- [Handler Result](message-result.md) — `IBenzeneResult` statuses, including `validation-error`
- [Middleware](middleware.md) — general middleware pipeline concepts
