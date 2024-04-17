using System.Collections.Generic;
using Benzene.Http;

namespace Benzene.Elements.Core;

public class ElementsHttpHeaderMappings : IHttpHeaderMappings
{
    public IDictionary<string, string> GetMappings()
    {
        return new Dictionary<string, string>
        {
            { "x-user-id", "userId" },
            { "x-correlation-id", "correlationId" },
            { "platformtenantid", "tenantId" },
            { "x-tenant-id", "tenantId" },
        };
    }
}