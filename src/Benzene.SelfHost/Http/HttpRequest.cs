namespace Benzene.SelfHost.Http;

public class HttpRequest
{
    public string Method { get; set; }
    public string Path { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();
    public string Body { get; set; }
}
