namespace Benzene.Http;

public class HttpRequest
{
    public string Method { get; set; }
    public string Path { get; set; }
    public IDictionary<string, string> Headers { get; set; }
}