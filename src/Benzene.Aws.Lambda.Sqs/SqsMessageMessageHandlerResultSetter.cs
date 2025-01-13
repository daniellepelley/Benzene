using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageMessageHandlerResultSetter : IMessageHandlerResultSetter<SqsMessageContext>
{
    public Task SetResultAsync(SqsMessageContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.IsSuccessful = messageHandlerResult.BenzeneResult.IsSuccessful;
        return Task.CompletedTask;
    }
}