using Benzene.Examples.AwsMesh.Inventory.Model;
using FluentValidation;

namespace Benzene.Examples.AwsMesh.Inventory.Validators;

/// <summary>
/// Validates the inbound <see cref="OrderPlaced"/> event — applied on every transport by the shared
/// wiring's <c>router.UseFluentValidation()</c>, so a malformed event is rejected the same way whether
/// it arrives over SNS, EventBridge, or direct invoke.
/// </summary>
public class OrderPlacedValidator : AbstractValidator<OrderPlaced>
{
    public OrderPlacedValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
