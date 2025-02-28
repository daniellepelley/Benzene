using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.SelfHost.Http;

public class KafkaMessageHandlerResultSetter : IMessageHandlerResultSetter<SelfHostHttpContext>
{
    public Task SetResultAsync(SelfHostHttpContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.HttpListenerContext.Response.StatusCode = 200;
        return Task.CompletedTask;
    }
}