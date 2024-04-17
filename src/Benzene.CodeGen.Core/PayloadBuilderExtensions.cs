using Benzene.Schema.OpenApi;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Benzene.CodeGen.Core;

public static class PayloadBuilderExtensions
{
    public static string BuildAsJson(this IPayloadBuilder source, OpenApiSchema openApiSchema, ISchemaGetter schemaGetter)
    {
        return JsonConvert.SerializeObject(source.Build(openApiSchema, schemaGetter));
    }
    
    public static IDictionary<string, object> Build(this IPayloadBuilder source, Type type)
    {
        var schemaBuilder = new SchemaBuilder();
        var schema = schemaBuilder.AddSchema(type);
        return source.Build(schema, new SchemaGetter(schemaBuilder.Build()));
    }

}
