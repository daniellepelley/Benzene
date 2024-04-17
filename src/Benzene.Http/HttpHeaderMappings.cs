namespace Benzene.Http;

public class HttpHeaderMappings : IHttpHeaderMappings
{
    private readonly IDictionary<string, string> _mappings;

    public HttpHeaderMappings(IDictionary<string, string> mappings)
    {
        _mappings = mappings;
    }
    public IDictionary<string, string> GetMappings()
    {
        return _mappings;
    }
}
