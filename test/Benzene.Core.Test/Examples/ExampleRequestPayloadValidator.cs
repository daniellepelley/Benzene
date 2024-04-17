using FluentValidation;

namespace Benzene.Test.Examples;

public class ExampleRequestPayloadValidator : AbstractValidator<ExampleRequestPayload>

{
    public ExampleRequestPayloadValidator()
    {
        RuleFor(x => x.Name).MaximumLength(10);
    }
}
