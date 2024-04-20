using Benzene.Abstractions.Mappers;

namespace Benzene.Azure.AspNet;

public class AspNetMessageHeadersMapper : IMessageHeadersMapper<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping = new Dictionary<string, string>
    {
        {"x-user-id", "userId" },
        {"x-correlation-id", "correlationId" },
    };

    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        return context.HttpRequest.Headers
            .Select(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant())
                ? (_headerMapping[x.Key.ToLowerInvariant()], context.HttpRequest.Headers[x.Key].First())
                : (x.Key, x.Value.First())
            )
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2.ToLowerInvariant());
    }
}
