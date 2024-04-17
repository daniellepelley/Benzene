using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public interface ITypeName
{
    string GetName(OpenApiSchema openApiSchema);
}
