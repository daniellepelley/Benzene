# Fluent Validation

`Benzene.FluentValidation` runs [FluentValidation](https://docs.fluentvalidation.net/) validators against a message handler's request before the handler executes, short-circuiting the pipeline with a `ValidationError` result when validation fails.

## Overview

FluentValidation lets you define strongly-typed validation rules for a request type using a fluent, lambda-based API (`AbstractValidator<T>`), separate from the request type itself and separate from the handler's business logic.

`Benzene.FluentValidation` plugs this into the middleware pipeline as a single piece of middleware, `ValidationMiddleware<TRequest, TResponse>`, added via `.UseFluentValidation()` inside `.UseMessageHandlers(...)`. For each request:

- If no `IValidator<TRequest>` is registered in DI, the middleware does nothing and calls the next middleware (which eventually invokes the handler).
- If a validator is found and the request is `null`, the pipeline short-circuits with a `ValidationError` result (`"Request is null"`).
- If a validator is found and validation fails, the pipeline short-circuits with the mapped status (`ValidationError` by default — see [Failure status mapping](#failure-status-mapping)) and the collected error messages.
- If validation passes, `next()` is called and the handler runs as normal.

Use this package when you want validation rules that live outside the request/handler, support complex cross-property rules, and can be unit-tested independently with FluentValidation's own test helpers.

## Installation

```bash
dotnet add package Benzene.FluentValidation
```

This pulls in the `FluentValidation` NuGet package (`11.8.0` at the time of writing) as a transitive dependency.

## Basic Usage

Define a validator for your request type:

```csharp
using FluentValidation;

public class CreateWidgetRequestValidator : AbstractValidator<CreateWidgetRequest>
{
    public CreateWidgetRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(10);
    }
}
```

Wire the middleware into the message handler pipeline:

```csharp
using Benzene.FluentValidation;

app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseMessageHandlers(router => router
        .UseFluentValidation()
    )
);
```

`.UseFluentValidation()` does two things:

1. Registers an `IValidationStatusMapper` (`DefaultValidationStatusMapper`, unless one is already registered) and discovers every `AbstractValidator<T>` in the given assemblies, registering each one against `IValidator<T>` with `TryAddSingleton` (see [Validator discovery](#validator-discovery)).
2. Adds `ValidationMiddleware<TRequest, TResponse>` to the handler pipeline, which resolves `IValidator<TRequest>` for the current request type on every call.

If `CreateWidgetRequest.Name` is longer than 10 characters, the handler is never invoked and the caller receives a `ValidationError` result containing FluentValidation's error messages.

## Validator discovery

`.UseFluentValidation()` has two overloads that control which assemblies/types are scanned for validators:

```csharp
// Scan specific assemblies
router.UseFluentValidation(typeof(CreateWidgetRequestValidator).Assembly);

// Scan specific types
router.UseFluentValidation(new[] { typeof(CreateWidgetRequestValidator) });

// No arguments: scans every assembly currently loaded in the AppDomain
router.UseFluentValidation();
```

Discovery works by finding every non-abstract type assignable to FluentValidation's `IValidator` (excluding types from the `FluentValidation` assembly itself), then registering each one as `IValidator<TRequest>` via `services.TryAddSingleton(...)`. Because `TryAddSingleton` is used:

- Validators are constructed once and reused for the lifetime of the application — they should not hold per-request mutable state.
- If you've already registered an `IValidator<TRequest>` yourself (e.g. `services.AddTransient<IValidator<CreateWidgetRequest>, CreateWidgetRequestValidator>()`), that registration wins and the scan will not overwrite it.

You can also register validators (and the schema builder) directly against the DI container without going through the router, using `AddFluentValidation`:

```csharp
using Benzene.FluentValidation;

services.UsingBenzene(x => x
    .AddFluentValidation(typeof(CreateWidgetRequestValidator).Assembly));
```

`ValidationMiddleware` resolves the validator per-request with `IServiceResolver.TryGetService<IValidator<TRequest>>()`, so a request type with no matching validator simply skips validation — it is not an error.

## Failure status mapping

By default, a failed validation maps to `IDefaultStatuses.ValidationError` — the same top-level
status every other validation failure in the pipeline uses (see [Handler Result](message-result)).
This is resolved by `IValidationStatusMapper.GetStatus(handlerType, requestType, validationResult)`,
implemented by `DefaultValidationStatusMapper`, which checks — in order:

1. **Per-rule status** — if any `ValidationFailure.CustomState` is a `BenzeneValidationState` (set via `.WithStatus(...)` on a rule), that status is used.
2. **Default** — falls back to `IDefaultStatuses.ValidationError`.

Deliberately, there is no per-handler or attribute-based override: a blanket, framework-provided
way for one handler to return a different status than another for the same kind of failure would
mean validation failures are inconsistent across the platform depending on which handler happened
to be hit. If you genuinely need one handler to behave differently, do it explicitly in that
handler's own code (return the status yourself, or add bespoke middleware) — see
[Overriding the default status platform-wide](#overriding-the-default-status-platform-wide) for the
sanctioned way to change what "the default" means everywhere at once.

### Overriding the status from a rule

```csharp
using FluentValidation;
using Benzene.FluentValidation;
using Benzene.Results;

public class SampleValidator : AbstractValidator<SampleRequest>
{
    public SampleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithStatus(BenzeneResultStatus.BadRequest);
    }
}
```

A failure on this rule returns `BenzeneResultStatus.BadRequest` instead of `ValidationError`.

### Overriding the default status platform-wide

`IDefaultStatuses` is the single, top-level place to change what status a validation failure (or a
not-found, bad-request, or unhandled-exception result) maps to across every handler and every
pipeline — register your own implementation before `AddBenzene()`'s `TryAddSingleton` runs
(or via `services.AddSingleton<IDefaultStatuses>(...)`, since the last registration wins) and every
handler that doesn't set a per-rule `.WithStatus(...)` picks it up:

```csharp
using Benzene.Core.MessageHandlers;
using Benzene.Results;

public class CustomDefaultStatuses : IDefaultStatuses
{
    public string ValidationError => BenzeneResultStatus.Forbidden;
    public string NotFound => BenzeneResultStatus.NotFound;
    public string BadRequest => BenzeneResultStatus.BadRequest;
    public string UnhandledException => BenzeneResultStatus.ServiceUnavailable;
}

services.AddSingleton<IDefaultStatuses, CustomDefaultStatuses>();
services.UsingBenzene(x => x.AddBenzene() /* ... */);
```

You can also supply a custom `IValidationStatusMapper` implementation entirely, instead of
`DefaultValidationStatusMapper`, by registering it before calling `.UseFluentValidation()`
(registration uses `TryAddSingleton`, so the first one registered wins).

## Composing inside `.UseMessageHandlers()`

`.UseFluentValidation()` is a handler-pipeline middleware builder (`IHandlerMiddlewareBuilder`) added inside the `router` passed to `.UseMessageHandlers(...)` — the same place a handler's own request/response types have been resolved, before the handler is invoked:

```csharp
app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseTimer("benzene-message")
    .UseLogResult(x => x.WithCorrelationId())
    .UseMessageHandlers(router => router
        .UseFluentValidation()
    )
);
```

Because it is per-handler middleware, it runs for every message handler in the pipeline that has a matching `IValidator<TRequest>` registered — there's no need to add it once per handler.

## Client-side validation

`Benzene.FluentValidation` also ships `ValidationClientMiddleware<TRequest, TResponse>`, which applies the same "resolve `IValidator<TRequest>`, validate, short-circuit with `ValidationError` on failure" behavior to outgoing Benzene client calls (`IBenzeneClientContext<TRequest, TResponse>`), rather than incoming message handler requests. Note that client-side failures always map to `BenzeneResultStatus.ValidationError` — the per-rule/per-handler status mapping described above only applies to `ValidationMiddleware` on the handler side.

## Custom string validators

The package includes a set of reusable FluentValidation rule extensions for common string checks, in `Benzene.FluentValidation.Common`:

```csharp
using FluentValidation;
using Benzene.FluentValidation.Common;

public class TestValidator : AbstractValidator<TestValidationObject>
{
    public TestValidator()
    {
        RuleFor(x => x.IsOneOf).IsOneOf("one", "two");
        RuleFor(x => x.IsAlphaNumericAndSymbols).IsAlphaNumericAndSymbols('!').NotEmpty().NotNull();
        RuleFor(x => x.IsBoolean).IsBoolean();
        RuleFor(x => x.IsDoubleGuid).IsDoubleGuid();
        RuleFor(x => x.IsGuid).IsGuid();
        RuleFor(x => x.IsJson).IsJson();
        RuleFor(x => x.IsLettersAndSymbols).IsLettersAndSymbols('!');
        RuleFor(x => x.IsNumbersAndSymbols).IsNumbersAndSymbols('!');
        RuleFor(x => x.IsNumeric).IsNumeric();
    }
}
```

Available extensions: `IsGuid()`, `IsDoubleGuid()` (two GUIDs separated by `|`), `IsNumeric()`, `IsJson()`, `IsOneOf(params string[] options)`, `IsStrictAlphabetic()`, `IsLettersAndSymbols(params char[] validChars)`, `IsAlphaNumericAndSymbols(params char[] validChars)`, `IsNumbersAndSymbols(params char[] validChars)`, and `IsBoolean()`.

## Validation schema (OpenAPI / documentation generation)

`AddFluentValidation` also registers an `IValidationSchemaBuilder` (`FluentValidationSchemaBuilder`) that reflects over a validator's rules to produce a description per property — used by Benzene's documentation/schema generation to describe validation rules (e.g. for OpenAPI output) without re-running FluentValidation itself:

```csharp
using Benzene.FluentValidation.Schema;

var schemaBuilder = new FluentValidationSchemaBuilder(new TestValidator());
var schemas = schemaBuilder.GetValidationSchemas(typeof(TestValidationObject));
// schemas["IsOneOf"][0].Name        == "IsOneOf"
// schemas["IsOneOf"][0].Description == "Is one of 'one', 'two'"
```

Recognized rule types include minimum/maximum length, regular expressions, `NotEmpty`/`NotNull`, `IsOneOf`, email, phone number, numeric comparisons (`GreaterThan`, `LessThanOrEqual`, etc.), and the custom string validators listed above. Rules it doesn't recognize are omitted from the schema rather than causing an error.

## See Also
- [Message Handlers](message-handlers) — where validation middleware sits in the request lifecycle
- [Handler Result](message-result) — `IBenzeneResult` statuses, including `ValidationError`
- [Middleware](middleware) — general middleware pipeline concepts
- [Data Annotations](data-annotations) — the attribute-based alternative to FluentValidation
