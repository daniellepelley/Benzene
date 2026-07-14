namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Utility functions for working with DynamoDB stream records.
/// </summary>
public static class DynamoDbUtils
{
    /// <summary>
    /// Parses the table name out of a stream ARN
    /// (<c>arn:aws:dynamodb:region:account:table/Name/stream/2015-06-27T00:48:05.899</c>).
    /// </summary>
    /// <param name="eventSourceArn">The record's stream ARN.</param>
    /// <returns>The table name, or null if the ARN is missing or not in the expected shape.</returns>
    /// <remarks>
    /// The resource segment contains colons of its own (the stream timestamp), so the ARN is split
    /// on <c>':'</c> with a maximum count of 6 before splitting the resource on <c>'/'</c>.
    /// </remarks>
    public static string GetTableName(string eventSourceArn)
    {
        if (string.IsNullOrEmpty(eventSourceArn))
        {
            return null;
        }

        var arnParts = eventSourceArn.Split(':', 6);
        if (arnParts.Length < 6)
        {
            return null;
        }

        var resourceParts = arnParts[5].Split('/');
        if (resourceParts.Length < 2 || resourceParts[0] != "table")
        {
            return null;
        }

        return resourceParts[1];
    }
}
