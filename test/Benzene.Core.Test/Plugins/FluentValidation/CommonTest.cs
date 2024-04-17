using System;
using FluentValidation.TestHelper;
using Benzene.Test.Plugins.FluentValidation.Examples;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class CommonTest
{
    private readonly TestValidator _validator = new();

    [Fact]
    public void Valid()
    {
        var obj = new TestValidationObject
        {
            IsOneOf = "one",
            IsAlphaNumericAndSymbols = "one!",
            IsBoolean = "true",
            IsDoubleGuid = $"{Guid.NewGuid()}|{Guid.NewGuid()}",
            IsGuid = $"{Guid.NewGuid()}",
            IsJson = "{}",
            IsLettersAndSymbols = "one!",
            IsNumbersAndSymbols = "12!",
            IsNumeric = "12",
        };
        var result = _validator.TestValidate(obj);

        result.ShouldNotHaveValidationErrorFor(x => x.IsOneOf);
        result.ShouldNotHaveValidationErrorFor(x => x.IsAlphaNumericAndSymbols);
        result.ShouldNotHaveValidationErrorFor(x => x.IsBoolean);
        result.ShouldNotHaveValidationErrorFor(x => x.IsDoubleGuid);
        result.ShouldNotHaveValidationErrorFor(x => x.IsGuid);
        result.ShouldNotHaveValidationErrorFor(x => x.IsJson);
        result.ShouldNotHaveValidationErrorFor(x => x.IsLettersAndSymbols);
        result.ShouldNotHaveValidationErrorFor(x => x.IsNumbersAndSymbols);
        result.ShouldNotHaveValidationErrorFor(x => x.IsNumeric);
    }

    [Fact]
    public void Invalid()
    {
        var obj = new TestValidationObject
        {
            IsOneOf = "foo",
            IsAlphaNumericAndSymbols = "foo*",
            IsBoolean = "foo",
            IsDoubleGuid = "foo",
            IsGuid = "foo",
            IsJson = "foo",
            IsLettersAndSymbols = "foo*",
            IsNumbersAndSymbols = "foo",
            IsNumeric = "foo",
        };
        var result = _validator.TestValidate(obj);

        result.ShouldHaveValidationErrorFor(x => x.IsOneOf);
        result.ShouldHaveValidationErrorFor(x => x.IsAlphaNumericAndSymbols);
        result.ShouldHaveValidationErrorFor(x => x.IsBoolean);
        result.ShouldHaveValidationErrorFor(x => x.IsDoubleGuid);
        result.ShouldHaveValidationErrorFor(x => x.IsGuid);
        result.ShouldHaveValidationErrorFor(x => x.IsJson);
        result.ShouldHaveValidationErrorFor(x => x.IsLettersAndSymbols);
        result.ShouldHaveValidationErrorFor(x => x.IsNumbersAndSymbols);
        result.ShouldHaveValidationErrorFor(x => x.IsNumeric);
    }
}
