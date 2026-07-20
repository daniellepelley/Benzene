using System.Net;
using Amazon.DynamoDBv2;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.DynamoDb;

/// <summary>
/// Verifies a DynamoDB table is reachable with a read-only <c>DescribeTable</c> call. Non-destructive
/// (metadata only); healthy on a 200 response, unhealthy on any error.
/// </summary>
public class DynamoDbHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="tableName">The table to check.</param>
    /// <param name="dynamoDb">The DynamoDB client used to describe the table.</param>
    public DynamoDbHealthCheck(string tableName, IAmazonDynamoDB dynamoDb)
    {
        _tableName = tableName;
        _dynamoDb = dynamoDb;
    }

    /// <inheritdoc />
    public string Type => "DynamoDb";

    /// <inheritdoc />
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Table", _tableName) };

        try
        {
            var response = await _dynamoDb.DescribeTableAsync(_tableName);
            var status = response.Table?.TableStatus?.Value;

            return HealthCheckResult.CreateInstance(response.HttpStatusCode == HttpStatusCode.OK, Type,
                new Dictionary<string, object> { { "TableName", _tableName }, { "TableStatus", status ?? "unknown" } },
                dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (table missing, no connectivity) are a failed result, not a throw;
            // report the exception type, never its message.
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "TableName", _tableName }, { "Error", ex.GetType().Name } },
                dependencies);
        }
    }
}
