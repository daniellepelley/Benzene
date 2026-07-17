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
- Failure status resolution (`DefaultValidationStatusMapper`) has exactly two branches: a per-rule
  `BenzeneValidationState.Status` (set via `.WithStatus(...)`, an explicit, deliberate override in
  application validator code), then `IDefaultStatuses.ValidationError` (`Benzene.Core.MessageHandlers`)
  as the default. There is deliberately **no per-handler or attribute-based override** — a
  framework-provided way for one handler to return a different status than another for the same
  kind of failure would make validation failures inconsistent across the platform. This package
  therefore takes a `ProjectReference` on `Benzene.Core.MessageHandlers` (mirroring
  `Benzene.JsonSchema`'s existing dependency on it for the same interface) purely to consume
  `IDefaultStatuses` in `DefaultValidationStatusMapper`'s constructor.

## Tests
- `Common/*Validator` (`IsGuid`, `IsBoolean`, `IsDoubleGuid`, `IsJson`, `IsNumeric`, `IsOneOf`,
  `IsLettersOrSymbols`, `IsNumbersOrSymbols`, `IsAlphaNumericAndSymbols`) - happy/invalid-value
  cases in `test/Benzene.Core.Test/Plugins/FluentValidation/CommonTest.cs` (note: these live under
  the `Benzene.FluentValidation.Common` namespace, not `Benzene.FluentValidation` - a grep for the
  latter alone will undercount this package's coverage), plus a dedicated case proving each
  validator's null/empty-bypasses-the-format-check contract (format validators only apply once
  there's a value; `.NotEmpty()`/`.NotNull()` must be chained explicitly, as `TestValidator` does
  for `IsAlphaNumericAndSymbols`).
- `DefaultValidationStatusMapper` - both status-resolution branches (per-failure
  `BenzeneValidationState.Status`, then the injected `IDefaultStatuses.ValidationError` default,
  including with an overridden `IDefaultStatuses`) are unit-tested directly in
  `DefaultValidationStatusMapperTest.cs`, in addition to end-to-end coverage via a real pipeline
  in `EnhancedFluentValidationTest.cs`.
- `Utils.GetAllTypes`/`GetAssemblies` (assembly/type discovery for auto-registering validators) -
  `UtilsTest.cs`.
