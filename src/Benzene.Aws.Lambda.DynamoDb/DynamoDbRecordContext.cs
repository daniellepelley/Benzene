namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Provides the middleware pipeline context for a single record within a DynamoDB Streams batch.
/// </summary>
public class DynamoDbRecordContext
{
    private DynamoDbRecordContext(DynamoDbEvent dynamoDbEvent, DynamoDbStreamRecord record)
    {
        DynamoDbEvent = dynamoDbEvent;
        Record = record;
    }

    /// <summary>
    /// Creates a new <see cref="DynamoDbRecordContext"/> for a single record within a stream batch.
    /// </summary>
    /// <param name="dynamoDbEvent">The full stream batch event.</param>
    /// <param name="record">The specific record within the batch this context represents.</param>
    /// <returns>The created context.</returns>
    public static DynamoDbRecordContext CreateInstance(DynamoDbEvent dynamoDbEvent, DynamoDbStreamRecord record)
    {
        return new DynamoDbRecordContext(dynamoDbEvent, record);
    }

    /// <summary>
    /// Gets the full stream batch event this record belongs to.
    /// </summary>
    public DynamoDbEvent DynamoDbEvent { get; }

    /// <summary>
    /// Gets the specific stream record this context represents.
    /// </summary>
    public DynamoDbStreamRecord Record { get; }

    /// <summary>
    /// Gets or sets whether this record was handled successfully. Set by
    /// <see cref="DynamoDbMessageMessageHandlerResultSetter"/>; null if no result has been set yet.
    /// </summary>
    public bool? IsSuccessful { get; set; }
}
