using Benzene.Abstractions.Mappers;
using Benzene.Http;

namespace Benzene.AspNet.Core;

public class AspNetHeadersToBodyMapper : IMessageHeadersMapper<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping;

    public AspNetHeadersToBodyMapper(IHttpHeaderMappings httpHeaderMappings)
    {
        _headerMapping = httpHeaderMappings.GetMappings();
    }

    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        return context.HttpContext.Request.Headers
            .Where(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant()))
            .Select(x => (_headerMapping[x.Key.ToLowerInvariant()], context.HttpContext.Request.Headers[x.Key].First()))
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}