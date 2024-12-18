namespace Benzene.JsonSchema;

public interface IJsonSchemaProvider<TContext>
{
    public Json.Schema.JsonSchema? Get(TContext context);
}