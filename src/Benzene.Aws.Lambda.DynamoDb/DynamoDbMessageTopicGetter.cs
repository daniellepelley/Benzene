using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Resolves the message topic as <c>"{tableName}:{eventName}"</c> (plan decision DS2), e.g.
/// <c>orders:INSERT</c> — the two things that identify a change-data-capture event: which table
/// and what happened. A handler declares <c>[Message("orders:INSERT")]</c>.
/// </summary>
public class DynamoDbMessageTopicGetter : IMessageTopicGetter<DynamoDbRecordContext>
{
    /// <summary>
    /// Gets the topic from the record's table name and event name.
    /// </summary>
    /// <param name="context">The DynamoDB record context to extract the topic from.</param>
    /// <returns>
    /// A topic of <c>"{tableName}:{eventName}"</c>, falling back to the bare event name when the
    /// stream ARN can't be parsed.
    /// </returns>
    public ITopic GetTopic(DynamoDbRecordContext context)
    {
        var tableName = DynamoDbUtils.GetTableName(context.Record.EventSourceArn);
        var eventName = context.Record.EventName;

        return new Topic(tableName != null ? $"{tableName}:{eventName}" : eventName);
    }
}
