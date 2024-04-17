using Benzene.Abstractions.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpMessageHeadersMapper : IMessageHeadersMapper<HttpContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public HttpMessageHeadersMapper(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    public IDictionary<string, string> GetHeaders(HttpContext context)
    {
        return DictionaryUtils.Replace(context.Request.Headers, _httpHeaderMappings.GetMappings());
    }
}
