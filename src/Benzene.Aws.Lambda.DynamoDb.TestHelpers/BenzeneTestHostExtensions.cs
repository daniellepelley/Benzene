using Benzene.Abstractions;

namespace Benzene.Aws.Lambda.DynamoDb.TestHelpers;

public static class BenzeneTestHostExtensions
{
    public static Task<DynamoDbBatchResponse> SendDynamoDbAsync(this IBenzeneTestHost source, DynamoDbEvent dynamoDbEvent)
    {
        return source.SendEventAsync<DynamoDbBatchResponse>(dynamoDbEvent);
    }

    public static Task<DynamoDbBatchResponse> SendDynamoDbAsync<T>(this IBenzeneTestHost source, IMessageBuilder<T> messageBuilder)
    {
        return source.SendDynamoDbAsync(messageBuilder.AsDynamoDb());
    }
}
