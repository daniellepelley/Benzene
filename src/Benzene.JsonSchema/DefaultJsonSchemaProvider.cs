namespace Benzene.JsonSchema;

public class DefaultJsonSchemaProvider<TContext> : IJsonSchemaProvider<TContext>
{
    public Json.Schema.JsonSchema? Get(TContext context) => null;
}