using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Records the message handler result onto a <see cref="DynamoDbRecordContext"/>'s
/// <see cref="DynamoDbRecordContext.MessageResult"/>, so <see cref="DynamoDbApplication"/> can stop
/// at the first failed record and report it back to Lambda for redelivery.
/// </summary>
public class DynamoDbMessageHandlerResultSetter : MessageHandlerResultSetterBase<DynamoDbRecordContext>;
