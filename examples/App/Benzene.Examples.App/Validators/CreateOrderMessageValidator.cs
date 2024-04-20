using Benzene.Examples.App.Model.Messages;
using FluentValidation;

namespace Benzene.Examples.App.Validators;

public class CreateOrderMessageValidator : AbstractValidator<CreateOrderMessage>
{
    public CreateOrderMessageValidator()
    {
        RuleFor(model => model.Status).NotEmpty().MaximumLength(49);
        RuleFor(model => model.Name).NotEmpty().MaximumLength(49).Unless(x => string.IsNullOrEmpty(x.Name));
    }
}