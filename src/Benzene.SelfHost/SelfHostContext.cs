namespace Benzene.SelfHost;

public class SelfHostContext
{
    public SelfHostContext(string request)
    {
        Request = request;
        Response = string.Empty;
    }

    public string Request { get; }
    public string Response { get; set; }
}

