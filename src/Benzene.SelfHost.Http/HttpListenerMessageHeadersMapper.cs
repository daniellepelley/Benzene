using Benzene.Abstractions.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageHeadersMapper : IMessageHeadersMapper<SelfHostHttpContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public HttpListenerMessageHeadersMapper(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    public IDictionary<string, string> GetHeaders(SelfHostHttpContext context)
    {
        return DictionaryUtils.Replace(context.HttpListenerContext.Request.Headers.ToDictionary(),
            _httpHeaderMappings.GetMappings());
    }

}