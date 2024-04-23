
using Benzene.Abstractions.Mappers;
using Benzene.Http;

namespace Benzene.AspNet.Core;

public class AspNetMessageHeadersMapper : IMessageHeadersMapper<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping;

    public AspNetMessageHeadersMapper(IHttpHeaderMappings httpHeaderMappings)
    {
        _headerMapping = httpHeaderMappings.GetMappings();
    }

    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        return context.HttpContext.Request.Headers
            .Select(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant())
                ? (_headerMapping[x.Key.ToLowerInvariant()], context.HttpContext.Request.Headers[x.Key].First())
                : (x.Key, x.Value.First())
            )
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}