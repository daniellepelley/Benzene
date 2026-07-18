using Benzene.Examples.AwsMesh.Payments.Handlers;
using FluentValidation;

namespace Benzene.Examples.AwsMesh.Payments.Validators;

/// <summary>Validates <see cref="CapturePayment"/> on every transport.</summary>
public class CapturePaymentValidator : AbstractValidator<CapturePayment>
{
    public CapturePaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
