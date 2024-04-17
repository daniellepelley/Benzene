namespace Benzene.Abstractions.Validation;

public interface IIsOneOfValidationSchema : IValidationSchema
{
    string[] Options { get; }
}