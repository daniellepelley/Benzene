using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public interface IPayloadBuilder
{
    IDictionary<string, object> Build(OpenApiSchema openApiSchema, ISchemaGetter schemaGetter);
}
