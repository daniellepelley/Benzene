# Benzene.FluentValidation

## What this package does
Runs [FluentValidation](https://fluentvalidation.net/) validators as Benzene pipeline middleware.
For each message it resolves the app's own `FluentValidation.IValidator<TRequest>` from DI and, if
one is registered, calls `ValidateAsync` before the handler runs; a failed validation short-circuits
with a validation-status result carrying the FluentValidation error messages. This package does
**not** define an `IValidator<T>` of its own - you write ordinary FluentValidation
`AbstractValidator<T>` classes and this wires them into the pipeline.

## Key types/interfaces
- `ValidationMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>`
  (Name `"FluentValidation"`) - the inbound (handler-side) validator. `TryGetService<IValidator<TRequest>>()`;
  no validator registered → pass through to `next()`. Null request → `BenzeneResult.Set<TResponse>(status, "Request is null")`.
  Invalid → `BenzeneResult.Set<TResponse>(status, validationResult.Errors.Select(e => e.ErrorMessage))`.
  The `status` comes from `IValidationStatusMapper`, not a hardcoded HTTP code.
- `ValidationMiddlewareBuilder : IHandlerMiddlewareBuilder` - builds the above per handler, resolving
  the registered `IValidationStatusMapper`.
- `ValidationClientMiddleware<TRequest, TResponse> : IMiddleware<IBenzeneClientContext<TRequest, TResponse>>`
  / `ValidationClientMiddlewareBuilder : IBenzeneClientContextMiddlewareBuilder` - the **outbound**
  counterpart, validating a request before it's sent via `IBenzeneClient`. This path has no status
  mapper - it always uses `BenzeneResult.ValidationError<TResponse>(...)`.
- `IValidationStatusMapper` (from `Benzene.Abstractions.Validation`) + `DefaultValidationStatusMapper`
  - resolves the result status in precedence order: (1) a per-failure `BenzeneValidationState.Status`
  (set on a rule via `.WithStatus(...)`), (2) a handler-level `[ValidationStatus]`
  (`ValidationStatusAttribute`), (3) the `BenzeneResultStatus.ValidationError` default.
- `FluentValidationExtensions.WithStatus(this IRuleBuilderOptions<T,TProperty>, string status)` +
  `BenzeneValidationState` - attach a Benzene result status to an individual rule via
  FluentValidation's `CustomState`.
- `DependencyExtensions` - `IMessageRouterBuilder.UseFluentValidation(params Assembly[])` /
  `UseFluentValidation(Type[])` scan for `IValidator` implementations, register each as its closed
  `IValidator<T>` singleton, register `DefaultValidationStatusMapper` and a
  `FluentValidationSchemaBuilder`, and add `ValidationMiddlewareBuilder`. `AddFluentValidation(...)`
  does the DI registration alone.
- `Common/` - reusable custom property validators and the `ExtensionMethods` rule-builder helpers
  that wrap them: `IsGuid()`, `IsDoubleGuid()`, `IsNumeric()`, `IsJson()`, `IsBoolean()`,
  `IsOneOf(params string[])`, `IsStrictAlphabetic()`, `IsLettersAndSymbols(params char[])`,
  `IsAlphaNumericAndSymbols(params char[])`, `IsNumbersAndSymbols(params char[])`. Each format
  validator **bypasses** null/empty values (a value only has to match the format once it's present),
  so pair them with `.NotEmpty()`/`.NotNull()` when the field is also required.
- `Schema/FluentValidationSchemaBuilder : IValidationSchemaBuilder` - reflects a validator's rules
  into `IValidationSchema`s (`MinLengthValidationSchema`, `MaxLengthValidationSchema`,
  `RegexValidationSchema`, `IsOneOfValidationSchema`, and the generic `ValidationSchema` for
  named constraints). `Benzene.Schema.OpenApi`'s `OpenApiValidationSchemaBuilder` consumes these to
  fold validation rules (min/max length, pattern, enum, required, formats) into generated OpenAPI
  property schemas.

## When to use this package
- When you want request validation with FluentValidation's fluent rule syntax and reusable validators.
- When validation rules should also surface in the generated OpenAPI/`benzene` spec (via the schema
  builder above) - pick this over `Benzene.DataAnnotations`, which has no schema-builder integration.
- When you need to map different validation failures to different Benzene result statuses
  (`.WithStatus(...)` / `[ValidationStatus]`).

## Dependencies on other Benzene packages
- **Benzene.Abstractions** / **Benzene.Abstractions.MessageHandlers** / **Benzene.Abstractions.Messages**
  - DI, message-handler context, client context.
- **Benzene.Abstractions.Middleware** - `IMiddleware<>`, the middleware-builder seams.
- **Benzene.Abstractions.Validation** - `IValidationStatusMapper`, `IValidationSchema`,
  `IValidationSchemaBuilder`, `ValidationStatusAttribute`, `ValidationConstants`.
- **Benzene.Results** - `BenzeneResult`, `BenzeneResultStatus`.
- **FluentValidation** (NuGet) - the validation engine itself.

## Important conventions
- You author `AbstractValidator<T>` classes; `UseFluentValidation` discovers and registers them. A
  request type with no registered validator is simply not validated (pass-through), not rejected.
- The result status is never a hardcoded HTTP code here - it flows through `IValidationStatusMapper`
  (default `ValidationError`), so the transport layer maps it to a wire status.
- Validation is async throughout (`ValidateAsync`).
- Format validators in `Common/` are null/empty-tolerant by design; chain `.NotEmpty()`/`.NotNull()`
  explicitly when the field is required.

## Tests
- `Common/*Validator` (`IsGuid`, `IsBoolean`, `IsDoubleGuid`, `IsJson`, `IsNumeric`, `IsOneOf`,
  `IsLettersOrSymbols`, `IsNumbersOrSymbols`, `IsAlphaNumericAndSymbols`) - happy/invalid-value
  cases in `test/Benzene.Core.Test/Plugins/FluentValidation/CommonTest.cs` (note: these live under
  the `Benzene.FluentValidation.Common` namespace, not `Benzene.FluentValidation` - a grep for the
  latter alone will undercount this package's coverage), plus a dedicated case proving each
  validator's null/empty-bypasses-the-format-check contract (format validators only apply once
  there's a value; `.NotEmpty()`/`.NotNull()` must be chained explicitly, as `TestValidator` does
  for `IsAlphaNumericAndSymbols`).
- `DefaultValidationStatusMapper` - all three status-resolution branches (per-failure
  `BenzeneValidationState.Status`, then handler-level `[ValidationStatus]`, then the
  `BenzeneResultStatus.ValidationError` default) are unit-tested directly in
  `DefaultValidationStatusMapperTest.cs`, in addition to the two branches already covered
  end-to-end via a real pipeline in `EnhancedFluentValidationTest.cs`.
- `Utils.GetAllTypes`/`GetAssemblies` (assembly/type discovery for auto-registering validators) -
  `UtilsTest.cs`.
