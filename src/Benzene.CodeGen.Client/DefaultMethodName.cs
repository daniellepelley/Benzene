using Benzene.CodeGen.Core;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

public class DefaultMethodName : IMethodName
{
    private readonly ITypeName _typeName;

    public DefaultMethodName(ITypeName typeName)
    {
        _typeName = typeName;
    }
    
    public string Create(string topic, OpenApiSchema requestSchema)
    {
        var typeName = _typeName.GetName(requestSchema);
        
        return typeName
            .Replace("Message", "")
            .Replace("Request", "");
    }
}
