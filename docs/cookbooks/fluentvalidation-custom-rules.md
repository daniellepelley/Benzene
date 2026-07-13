# FluentValidation with Custom Rules

Write complex custom FluentValidation rules for Benzene message handlers — cross-field checks,
async validation against a database, Benzene's built-in string-format rules, and per-rule status
overrides.

## Problem Statement

[Fluent Validation](../fluent-validation.md) covers the mechanics of wiring `ValidationMiddleware`
into the pipeline and the basics of `.WithStatus(...)`. This cookbook goes further, into validation
that real handlers actually need:

- A rule that compares two properties on the same request (e.g. an end date must be after a start
  date).
- A rule that has to call a database or service asynchronously (e.g. "this name is already taken"),
  which needs a constructor-injected dependency — and a real gotcha in how Benzene discovers
  validators that you need to know about before you hit it in production.
- Benzene's own reusable string-format rules (`IsGuid()`, `IsOneOf(...)`, etc.) from
  `Benzene.FluentValidation.Common`.
- Mapping a specific business rule to a specific result status — a duplicate-name failure should
  come back as `409 Conflict`, not the generic `422 ValidationError` every other failure gets.

## Prerequisites

- A Benzene message handler and request type already defined — see
  [Message Handlers](../message-handlers.md).
- `.UseFluentValidation()` already wired into your handler pipeline — see
  [Fluent Validation](../fluent-validation.md#basic-usage) for the base setup this cookbook builds
  on.

## Installation

```bash
dotnet add package Benzene.FluentValidation
```

This pulls in `FluentValidation` (`11.8.0` at the time of writing) transitively.

## Step-by-Step Implementation

### 1. The request and handler

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

public class CreateProductRequest
{
    public string Name { get; set; }
    public string Sku { get; set; }
    public DateTime LaunchDate { get; set; }
    public DateTime? DiscontinueDate { get; set; }
}

public class CreateProductResponse
{
    public Guid Id { get; set; }
}

[Message("product:create")]
[HttpEndpoint("POST", "/products")]
public class CreateProductHandler : IMessageHandler<CreateProductRequest, CreateProductResponse>
{
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IBenzeneResult<CreateProductResponse>> HandleAsync(CreateProductRequest request)
    {
        var id = await _productRepository.CreateAsync(request.Name, request.Sku, request.LaunchDate, request.DiscontinueDate);
        return BenzeneResult.Created(new CreateProductResponse { Id = id });
    }
}
```

### 2. Cross-field validation

Standard FluentValidation, nothing Benzene-specific: `RuleFor(x => x)` gives you the whole request,
so `.Must(...)` can compare two of its properties. `.WithName(...)` controls which property name
the resulting error is attributed to:

```csharp
using FluentValidation;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Sku).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.DiscontinueDate == null || x.DiscontinueDate > x.LaunchDate)
            .WithMessage("DiscontinueDate must be after LaunchDate")
            .WithName(nameof(CreateProductRequest.DiscontinueDate));
    }
}
```

### 3. Benzene's custom string rules

`Benzene.FluentValidation.Common` ships reusable rules for common string formats — see
[Custom string validators](../fluent-validation.md#custom-string-validators) for the full list.
They compose with built-ins like any other rule:

```csharp
using Benzene.FluentValidation.Common;

RuleFor(x => x.Sku).NotEmpty().IsGuid();
RuleFor(x => x.Name).NotEmpty().MaximumLength(50).IsAlphaNumericAndSymbols(' ', '-');
```

`IsAlphaNumericAndSymbols(params char[] validChars)` allows letters, digits, and exactly the
characters you list (here, space and hyphen) — useful for names that need to reject punctuation you
haven't explicitly allowed.

### 4. Async validation against a database — and a real gotcha

A "this name already exists" check needs to call a repository, which means the validator needs a
constructor dependency:

```csharp
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .IsAlphaNumericAndSymbols(' ', '-');

        RuleFor(x => x.Sku).NotEmpty().IsGuid();

        RuleFor(x => x)
            .Must(x => x.DiscontinueDate == null || x.DiscontinueDate > x.LaunchDate)
            .WithMessage("DiscontinueDate must be after LaunchDate")
            .WithName(nameof(CreateProductRequest.DiscontinueDate));

        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) => !await productRepository.NameExistsAsync(name, cancellationToken))
            .WithMessage(x => $"A product named '{x.Name}' already exists");
    }
}
```

This looks like it should just work with `.UseFluentValidation()`'s automatic discovery — but it
doesn't, and it's worth understanding exactly why before you build around it.

**`.UseFluentValidation(assemblies)` (and the parameterless `.UseFluentValidation()`) construct
every discovered validator with `Activator.CreateInstance`, unconditionally, even though DI
registration itself would happily support constructor injection.** This is in
`AddFluentValidation` in `src/Benzene.FluentValidation/DependencyExtensions.cs`:

```csharp
public static IBenzeneServiceContainer AddFluentValidation(this IBenzeneServiceContainer services, Type[] types)
{
    services.TryAddSingleton<IValidationStatusMapper, DefaultValidationStatusMapper>();

    var validatorTypes = types
        .Where(t => typeof(IValidator).IsAssignableFrom(t) && !t.IsAbstract && ...)
        .ToArray();

    foreach (var validatorType in validatorTypes)
    {
        services.TryAddSingleton(validatorType.GetInterface("IValidator`1"), validatorType);
    }

    var validators = validatorTypes
        .Select(x => Activator.CreateInstance(x) as IValidator)   // <-- here
        .ToArray();

    services.TryAddSingleton<IValidationSchemaBuilder>(new FluentValidationSchemaBuilder(validators));
    return services;
}
```

That `Activator.CreateInstance(x)` call exists to build the `FluentValidationSchemaBuilder` used
for schema/OpenAPI generation (see [Validation schema](../fluent-validation.md#validation-schema-openapi--documentation-generation)),
and it runs for *every* type the scan matches, regardless of whether that validator is ever
actually used through DI. A validator whose only constructor takes a dependency has no parameterless
constructor, so this throws at startup:

```
System.MissingMethodException: Cannot dynamically create an instance of type
'YourNamespace.CreateProductRequestValidator'. Reason: No parameterless constructor defined.
```

(Verified directly against the current source: constructing a validator with a required
constructor parameter through `AddFluentValidation` reproduces this exact exception.)

**The fix**: don't let a validator with constructor dependencies be included in whatever
assembly/type list you scan. Register it directly with your DI container instead — `ValidationMiddleware`
resolves `IValidator<TRequest>` with `_serviceResolver.TryGetService<IValidator<TRequest>>()`, so it
doesn't care whether the registration came from the scan or from you:

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IProductRepository, ProductRepository>();

    // Registered directly -- deliberately NOT covered by the UseFluentValidation scan below,
    // because its constructor dependency would crash AddFluentValidation's schema-builder scan.
    services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();

    services.UsingBenzene(x => x
        .AddMessageHandlers(typeof(CreateProductHandler).Assembly));
}

public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
{
    app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
        .UseMessageHandlers(router => router
            // Explicit type list, not an assembly scan: only validators with a public
            // parameterless constructor belong here. CreateProductRequestValidator is
            // deliberately excluded -- it's registered above instead.
            .UseFluentValidation(new[] { typeof(UpdateProductRequestValidator) })
        ));
}
```

If you pass an assembly to `.UseFluentValidation(assembly)` (or call `.UseFluentValidation()` with
no arguments, which scans every loaded assembly), the scan has no way to skip one type in that
assembly — it matches every non-abstract `IValidator` it finds. So the practical rule is: **keep
validators with constructor dependencies out of whatever assembly/type list you scan, and register
them directly.** Validators with a plain parameterless constructor (like a
`UpdateProductRequestValidator` with no external dependencies) are unaffected and can still go
through the scan as usual.

### 5. Returning `409 Conflict` for the duplicate-name rule

A duplicate name is a business-rule conflict, not a generic validation error — map it explicitly
with `.WithStatus(...)`, chained after the rule method (`.WithStatus` is only available on
`IRuleBuilderOptions<T, TProperty>`, which `MustAsync`, like `NotEmpty`/`Must`/every other rule
method, returns):

```csharp
using Benzene.FluentValidation;
using Benzene.Results;

RuleFor(x => x.Name)
    .MustAsync(async (name, cancellationToken) => !await productRepository.NameExistsAsync(name, cancellationToken))
    .WithMessage(x => $"A product named '{x.Name}' already exists")
    .WithStatus(BenzeneResultStatus.Conflict);
```

Every other rule on `CreateProductRequest` still falls back to the default
`BenzeneResultStatus.ValidationError` if it fails — `.WithStatus(...)` only overrides the status
for the rule it's attached to (`DefaultValidationStatusMapper` returns on the first failed rule
whose `CustomState` carries a status; see [Failure status mapping](../fluent-validation.md#failure-status-mapping)).

If your handler is exposed over HTTP (`Benzene.Http`), `BenzeneResultStatus.Conflict` maps to an
actual `409` response — `DefaultHttpStatusCodeMapper` maps `Conflict` → `"409"` alongside the rest
of the standard REST-ish mapping (`ValidationError` → `422`, `BadRequest` → `400`, and so on).

### Putting it together

```csharp
using Benzene.FluentValidation;
using Benzene.FluentValidation.Common;
using Benzene.Results;
using FluentValidation;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .IsAlphaNumericAndSymbols(' ', '-');

        RuleFor(x => x.Sku)
            .NotEmpty()
            .IsGuid();

        RuleFor(x => x)
            .Must(x => x.DiscontinueDate == null || x.DiscontinueDate > x.LaunchDate)
            .WithMessage("DiscontinueDate must be after LaunchDate")
            .WithName(nameof(CreateProductRequest.DiscontinueDate));

        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) => !await productRepository.NameExistsAsync(name, cancellationToken))
            .WithMessage(x => $"A product named '{x.Name}' already exists")
            .WithStatus(BenzeneResultStatus.Conflict);
    }
}
```

## Testing

Use FluentValidation's own `TestValidate` helpers for the validator in isolation — the pattern used
throughout `test/Benzene.Core.Test/Plugins/FluentValidation/`:

```csharp
using FluentValidation.TestHelper;
using Moq;
using Xunit;

public class CreateProductRequestValidatorTest
{
    [Fact]
    public async Task RejectsDuplicateName()
    {
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(x => x.NameExistsAsync("Widget", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var validator = new CreateProductRequestValidator(mockRepository.Object);
        var request = new CreateProductRequest { Name = "Widget", Sku = Guid.NewGuid().ToString(), LaunchDate = DateTime.UtcNow };

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task RejectsDiscontinueDateBeforeLaunchDate()
    {
        var mockRepository = new Mock<IProductRepository>();
        mockRepository.Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var validator = new CreateProductRequestValidator(mockRepository.Object);
        var request = new CreateProductRequest
        {
            Name = "Widget",
            Sku = Guid.NewGuid().ToString(),
            LaunchDate = DateTime.UtcNow,
            DiscontinueDate = DateTime.UtcNow.AddDays(-1)
        };

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.DiscontinueDate);
    }
}
```

To confirm the status mapping end-to-end through the real middleware pipeline, follow the pattern
in `test/Benzene.Core.Test/Plugins/FluentValidation/EnhancedFluentValidationTest.cs` — register the
validator, run a request through `BenzeneMessageApplication`, and assert on the response's
`StatusCode`:

```csharp
var serviceCollection = ServiceResolverMother.CreateServiceCollection();
serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
serviceCollection.AddTransient<CreateProductHandler>();
serviceCollection.AddSingleton(mockProductRepository.Object);
serviceCollection.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();

var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
container.AddFluentValidation(Array.Empty<Type>()); // registers IValidationStatusMapper; nothing to scan

var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
pipeline.UseMessageHandlers(x => x
    .UseFluentValidation(Array.Empty<Type>())
    .AddMessageHandler<CreateProductHandler, CreateProductRequest, CreateProductResponse>("product:create"));

var app = new BenzeneMessageApplication(pipeline.Build());
var response = await app.HandleAsync(
    new BenzeneMessageRequest { Topic = "product:create", Body = "{\"Name\":\"Widget\",\"Sku\":\"" + Guid.NewGuid() + "\"}" },
    new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

Assert.Equal(BenzeneResultStatus.Conflict, response.StatusCode); // when the repository reports the name already exists
```

## Troubleshooting

### `MissingMethodException: ... No parameterless constructor defined` on startup

You've passed a validator with a constructor dependency into `.UseFluentValidation(assembly)`,
`.UseFluentValidation(types)`, or the parameterless `.UseFluentValidation()` (which scans every
loaded assembly). See [step 4](#4-async-validation-against-a-database--and-a-real-gotcha) above —
exclude that validator from the scanned assembly/type list and register it directly with
`services.AddScoped<IValidator<TRequest>, YourValidator>()` (or `AddSingleton`/`AddTransient`,
matching your dependency's own lifetime — a scoped repository needs at least a scoped validator
registration, since a singleton validator cannot consume a scoped service).

### The validator never runs

`ValidationMiddleware` resolves `IValidator<TRequest>` for the exact request type your handler
declares (`IMessageHandler<TRequest, TResponse>`). Double-check the validator is
`AbstractValidator<TRequest>` for that exact type, not a base or derived type, and that it's
actually registered — either scanned (parameterless constructor, present in the scanned
assembly/type list) or manually registered (as described above).

### `.WithStatus(...)` doesn't compile / doesn't apply

`WithStatus` extends `IRuleBuilderOptions<T, TProperty>`, which every FluentValidation rule method
(`NotEmpty()`, `Must(...)`, `MustAsync(...)`, `MaximumLength(...)`, etc.) returns — so it has to be
chained directly after one of those, not called straight off `RuleFor(...)`. If two rules on the
same request both set a custom status and both fail, `DefaultValidationStatusMapper` returns the
status of whichever failure appears first in FluentValidation's own `ValidationResult.Errors`
(effectively rule declaration order for synchronous rules) — it does not merge or prioritize
between them.

### Client-side validation always returns `ValidationError`, ignoring `.WithStatus(...)`

This is expected, not a bug: `ValidationClientMiddleware` (used for outgoing Benzene client calls)
always maps failures to `BenzeneResultStatus.ValidationError`. The per-rule/per-handler status
mapping described here only applies to `ValidationMiddleware` on the incoming handler side — see
[Client-side validation](../fluent-validation.md#client-side-validation).

## Variations

### Keeping rules modular without re-triggering the scan issue

If a single validator with a constructor dependency grows large, you can still keep the
plain, dependency-free rules in a separate reusable validator class and compose it in via
FluentValidation's own `Include(...)`, without registering that second class as `IValidator<T>` in
DI at all (so it never needs to go through discovery, scanned or otherwise):

```csharp
public class CreateProductRequestFormatValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestFormatValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50).IsAlphaNumericAndSymbols(' ', '-');
        RuleFor(x => x.Sku).NotEmpty().IsGuid();
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IProductRepository productRepository)
    {
        Include(new CreateProductRequestFormatValidator());

        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) => !await productRepository.NameExistsAsync(name, ct))
            .WithStatus(BenzeneResultStatus.Conflict);
    }
}
```

Only `CreateProductRequestValidator` is ever registered as `IValidator<CreateProductRequest>`, so
this sidesteps the discovery/scan issue entirely while keeping the two rule sets separately
testable.

### DataAnnotations instead

If you'd rather not take the FluentValidation dependency at all, `Benzene.DataAnnotations` is the
attribute-based alternative — see [Data Annotations](../data-annotations.md). It doesn't support
per-rule status overrides or async rules the way `Benzene.FluentValidation` does.

## Further Reading

- [Fluent Validation](../fluent-validation.md) — the middleware mechanics, validator discovery,
  and failure status mapping this cookbook builds on
- [Message Handlers](../message-handlers.md) — `[Message]`, `IMessageHandler<TRequest, TResponse>`
- [Handler Result](../message-result.md) — `IBenzeneResult` statuses, including `Conflict` and
  `ValidationError`
- [Data Annotations](../data-annotations.md) — the attribute-based alternative to FluentValidation
