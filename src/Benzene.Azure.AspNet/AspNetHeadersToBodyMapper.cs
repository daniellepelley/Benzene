using Benzene.Abstractions.Mappers;

namespace Benzene.Azure.AspNet;

public class AspNetHeadersToBodyMapper : IMessageHeadersMapper<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping = new Dictionary<string, string>
    {
        {"x-user-id", "userId" },
    };

    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        return context.HttpRequest.Headers
            .Where(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant()))
            .Select(x => (_headerMapping[x.Key.ToLowerInvariant()], context.HttpRequest.Headers[x.Key].First()))
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}
