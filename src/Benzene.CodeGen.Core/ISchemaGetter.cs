using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public interface ISchemaGetter
{
    OpenApiSchema GetOpenApiSchema(OpenApiSchema openApiSchema);
    OpenApiSchema GetOpenApiSchema(string id);
}
