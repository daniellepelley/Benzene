using System;
using FluentValidation;
using FluentValidation.Validators;

namespace Benzene.Examples.App.Helper;

public class IsGuidValidator<T> : PropertyValidator<T, string>
{

    public override string Name => "IsGuidValidator";

    public override bool IsValid(ValidationContext<T> context, string value)
    {
        return Guid.TryParse(value, out Guid _);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return Localized(errorCode, Name);
    }
}