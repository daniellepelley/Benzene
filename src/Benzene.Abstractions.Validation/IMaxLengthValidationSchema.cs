namespace Benzene.Abstractions.Validation;

public interface IMaxLengthValidationSchema : IValidationSchema
{
    int Max { get; }
}