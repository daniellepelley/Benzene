namespace Benzene.Abstractions.Validation;

public interface IValidationStatusMapper
{
    string GetStatus(Type? handlerType, Type requestType, object? result);
}
