namespace Benzene.Abstractions.Validation;

public interface IValidationSchemaBuilder
{
    IDictionary<string, IValidationSchema[]> GetValidationSchemas(Type type);
}