using Benzene.Examples.AwsMesh.Orders.Handlers;
using FluentValidation;

namespace Benzene.Examples.AwsMesh.Orders.Validators;

/// <summary>
/// Validates <see cref="CreateOrder"/> — applied on every transport (HTTP, invoke, SQS, SNS,
/// EventBridge) by the shared wiring's <c>router.UseFluentValidation()</c>, so an invalid payload is
/// rejected the same way no matter how the order arrives.
/// </summary>
public class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Item).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}
