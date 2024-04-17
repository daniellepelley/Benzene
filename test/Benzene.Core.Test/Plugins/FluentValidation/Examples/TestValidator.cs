using FluentValidation;
using Benzene.FluentValidation.Common;

namespace Benzene.Test.Plugins.FluentValidation.Examples;

public class TestValidator : AbstractValidator<TestValidationObject>
{
    public TestValidator()
    {
        RuleFor(x => x.IsOneOf).IsOneOf("one", "two");
        RuleFor(x => x.IsAlphaNumericAndSymbols).IsAlphaNumericAndSymbols('!').NotEmpty().NotNull();
        RuleFor(x => x.IsBoolean).IsBoolean();
        RuleFor(x => x.IsDoubleGuid).IsDoubleGuid();
        RuleFor(x => x.IsGuid).IsGuid();
        RuleFor(x => x.IsJson).IsJson();
        RuleFor(x => x.IsLettersAndSymbols).IsLettersAndSymbols('!');
        RuleFor(x => x.IsNumbersAndSymbols).IsNumbersAndSymbols('!');
        RuleFor(x => x.IsNumeric).IsNumeric();
        RuleFor(x => x.MinLength).MinimumLength(0);
        RuleFor(x => x.MaxLength).MaximumLength(10);
    }
}
