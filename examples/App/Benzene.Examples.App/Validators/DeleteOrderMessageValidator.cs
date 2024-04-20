using Benzene.Examples.App.Model.Messages;
using FluentValidation;

namespace Benzene.Examples.App.Validators;

public class DeleteOrderMessageValidator : AbstractValidator<DeleteOrderMessage>
{
    public DeleteOrderMessageValidator()
    {
        RuleFor(model => model.Id).NotEmpty();
    }
}