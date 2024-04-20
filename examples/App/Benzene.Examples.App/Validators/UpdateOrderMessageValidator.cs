using Benzene.Examples.App.Model.Messages;
using FluentValidation;

namespace Benzene.Examples.App.Validators;

public class UpdateOrderMessageValidator : AbstractValidator<UpdateOrderMessage>
{
    public UpdateOrderMessageValidator()
    {
        RuleFor(model => model.Id).NotEmpty();
        RuleFor(model => model.Status).MaximumLength(49);
        RuleFor(model => model.Name).MaximumLength(49);
    }
}