using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Records whether a message handler result was successful onto the DynamoDB record context, so
/// <see cref="DynamoDbApplication"/> can stop at the first failed record and report it back to
/// Lambda for redelivery.
/// </summary>
public class DynamoDbMessageMessageHandlerResultSetter : IMessageHandlerResultSetter<DynamoDbRecordContext>
{
    /// <summary>
    /// Sets the success flag on the context from the message handler result.
    /// </summary>
    /// <param name="context">The DynamoDB record context to record the result on.</param>
    /// <param name="messageHandlerResult">The result produced by the message handler.</param>
    /// <returns>A completed task.</returns>
    public Task SetResultAsync(DynamoDbRecordContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.IsSuccessful = messageHandlerResult.BenzeneResult.IsSuccessful;
        return Task.CompletedTask;
    }
}
