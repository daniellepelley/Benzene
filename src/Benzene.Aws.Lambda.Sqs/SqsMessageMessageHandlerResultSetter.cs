using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Records whether a message handler result was successful onto the SQS context, so
/// <see cref="SqsApplication"/> can report failed records back to SQS for retry.
/// </summary>
public class SqsMessageMessageHandlerResultSetter : IMessageHandlerResultSetter<SqsMessageContext>
{
    /// <summary>
    /// Sets the success flag on the context from the message handler result.
    /// </summary>
    /// <param name="context">The SQS message context to record the result on.</param>
    /// <param name="messageHandlerResult">The result produced by the message handler.</param>
    /// <returns>A completed task.</returns>
    public Task SetResultAsync(SqsMessageContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.IsSuccessful = messageHandlerResult.BenzeneResult.IsSuccessful;
        return Task.CompletedTask;
    }
}
