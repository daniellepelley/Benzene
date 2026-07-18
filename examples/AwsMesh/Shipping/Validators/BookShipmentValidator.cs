using Benzene.Examples.AwsMesh.Shipping.Handlers;
using FluentValidation;

namespace Benzene.Examples.AwsMesh.Shipping.Validators;

/// <summary>Validates <see cref="BookShipment"/> on every transport.</summary>
public class BookShipmentValidator : AbstractValidator<BookShipment>
{
    public BookShipmentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Carrier).NotEmpty().Must(c => c is "DPD" or "RoyalMail" or "UPS" or "FedEx")
            .WithMessage("Carrier must be one of: DPD, RoyalMail, UPS, FedEx.");
    }
}
