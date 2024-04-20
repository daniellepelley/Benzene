using Benzene.Examples.App.Helper;
using Benzene.Examples.App.Model.Messages;
using FluentValidation;

namespace Benzene.Examples.App.Validators;

public class GetOrderMessageValidator : AbstractValidator<GetOrderMessage>
{
    public GetOrderMessageValidator()
    {
        RuleFor(model => model.Id).NotEmpty().IsGuid();
    }
}