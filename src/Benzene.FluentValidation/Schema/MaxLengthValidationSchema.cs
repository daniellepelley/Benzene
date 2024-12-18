using FluentValidation.Validators;
using Benzene.Abstractions.Validation;

namespace Benzene.FluentValidation.Schema;

public class MaxLengthValidationSchema : ValidationSchema, IMaxLengthValidationSchema
{
    public int Max { get; }

    public MaxLengthValidationSchema(IMaximumLengthValidator maximumLengthValidator)
        : base(ValidationConstants.MaxLength, $"Maximum Length of {maximumLengthValidator.Max} characters")
    {
        Max = maximumLengthValidator.Max;
    }
}
