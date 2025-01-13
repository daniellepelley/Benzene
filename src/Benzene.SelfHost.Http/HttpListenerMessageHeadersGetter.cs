using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageHeadersGetter : IMessageHeadersGetter<SelfHostHttpContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public HttpListenerMessageHeadersGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    public IDictionary<string, string> GetHeaders(SelfHostHttpContext context)
    {
        return DictionaryUtils.Replace(context.HttpListenerContext.Request.Headers.ToDictionary(),
            _httpHeaderMappings.GetMappings());
    }

}