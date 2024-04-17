namespace Benzene.Http;

public interface IHttpHeaderMappings
{
    IDictionary<string, string> GetMappings();
}