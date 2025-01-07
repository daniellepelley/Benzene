namespace Benzene.Client.Http;

public class HttpSendMessageContext
{
    public HttpSendMessageContext(HttpRequestMessage request)
    {
        Request = request;
    }
    public HttpRequestMessage Request { get; }
    public HttpResponseMessage Response { get; set; }
}