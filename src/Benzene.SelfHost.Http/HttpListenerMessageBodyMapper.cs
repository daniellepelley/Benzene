using Benzene.Abstractions.Mappers;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageBodyMapper : IMessageBodyMapper<SelfHostHttpContext>
{
    public string GetBody(SelfHostHttpContext context)
    {
        using var reader = new StreamReader(context.HttpListenerContext.Request.InputStream);
        return reader.ReadToEnd();
    }
}


