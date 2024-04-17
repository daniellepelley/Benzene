namespace Benzene.Abstractions.Validation;

public interface IRegexValidationSchema : IValidationSchema
{
    string Expression { get; }
}