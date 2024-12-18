using Benzene.JsonSchema;

namespace Benzene.Test.Plugins.JsonSchema;

public class SimpleJsonSchemaProvider<TContext> : IJsonSchemaProvider<TContext>
{
    private readonly Json.Schema.JsonSchema _jsonSchema;

    public SimpleJsonSchemaProvider(Json.Schema.JsonSchema jsonSchema)
    {
        _jsonSchema = jsonSchema;
    }

    public Json.Schema.JsonSchema Get(TContext context)
    {
        return _jsonSchema;
    }
}