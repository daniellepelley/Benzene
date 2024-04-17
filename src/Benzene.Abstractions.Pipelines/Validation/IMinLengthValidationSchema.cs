namespace Benzene.Abstractions.Validation;

public interface IMinLengthValidationSchema : IValidationSchema
{
    int Min { get; }
}