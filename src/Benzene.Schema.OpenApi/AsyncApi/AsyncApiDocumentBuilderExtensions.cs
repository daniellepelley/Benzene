namespace Benzene.Schema.OpenApi.AsyncApi;

public static class AsyncApiDocumentBuilderExtensions
{
    public static AsyncApiDocumentBuilder AddJsonEvent(this AsyncApiDocumentBuilder source, string topic, string typeName, string json)
    {
        var jsonOpenApiSchemaBuilder = new JsonOpenApiSchemaBuilder();
    
        var schemas = jsonOpenApiSchemaBuilder.CreateSchema(typeName, json);
        source.AddEventDefinition(topic, typeName, schemas[typeName]);

        foreach (var schema in schemas.Where(x => x.Key != typeName))
        {
            source.AddSchema(schema.Key, schema.Value);
        }
             
        return source;
    } 
}
