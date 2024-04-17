using FluentValidation.Validators;
using Benzene.Abstractions.Validation;
using Benzene.Core.Validation;

namespace Benzene.FluentValidation.Schema;

public class MinLengthValidationSchema : ValidationSchema, IMinLengthValidationSchema
{
    public int Min { get; }

    public MinLengthValidationSchema(IMinimumLengthValidator minimumLengthValidator)
        : base(ValidationConstants.MinLength, $"Minimum Length of {minimumLengthValidator.Min} characters")
    {
        Min = minimumLengthValidator.Min;
    }
}
