using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;

namespace Benzene.Aws.Sqs;

public class SqsMessageResultSetter : IResultSetter<SqsMessageContext>
{
    public void SetResult(SqsMessageContext context, IResult result, ITopic topic,
        IMessageHandlerDefinition messageHandlerDefinition)
    {
        context.IsSuccessful = result.IsSuccessful;
    }
}