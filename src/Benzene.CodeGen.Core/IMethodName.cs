using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Core;

public interface IMethodName
{
    string Create(string topic, OpenApiSchema requestSchema);
}
