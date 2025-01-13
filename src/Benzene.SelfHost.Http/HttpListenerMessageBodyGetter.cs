using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageBodyGetter : IMessageBodyGetter<SelfHostHttpContext>
{
    public string GetBody(SelfHostHttpContext context)
    {
        using var reader = new StreamReader(context.HttpListenerContext.Request.InputStream);
        return reader.ReadToEnd();
    }
}


