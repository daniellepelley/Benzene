namespace Benzene.Http;
public class DefaultHttpHeaderMappings : IHttpHeaderMappings
{
    public IDictionary<string, string> GetMappings()
    {
        return new Dictionary<string, string>();
    }
}
