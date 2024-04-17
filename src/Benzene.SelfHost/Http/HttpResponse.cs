namespace Benzene.SelfHost.Http;

public class HttpResponse
{
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public int StatusCode { get; set; }
    public string Body { get; set; }
    public string Version { get; set; }
    public string ReasonPhrase { get; set; }
}
