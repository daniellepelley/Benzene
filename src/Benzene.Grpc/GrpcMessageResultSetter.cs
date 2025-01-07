using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageResultSetter : IResultSetter<GrpcContext>
{
    public Task SetResultAsync(GrpcContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.ResponseAsObject = messageHandlerResult.BenzeneResult.PayloadAsObject;
        return Task.CompletedTask;
    }
}