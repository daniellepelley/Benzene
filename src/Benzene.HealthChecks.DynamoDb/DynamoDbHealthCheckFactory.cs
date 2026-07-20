using Amazon.DynamoDBv2;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.DynamoDb;

/// <summary>Builds a <see cref="DynamoDbHealthCheck"/> for a fixed table, resolving <see cref="IAmazonDynamoDB"/> from DI.</summary>
public class DynamoDbHealthCheckFactory : IHealthCheckFactory
{
    private readonly string _tableName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="tableName">The table the resulting health check will describe.</param>
    public DynamoDbHealthCheckFactory(string tableName)
    {
        _tableName = tableName;
    }

    /// <inheritdoc />
    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new DynamoDbHealthCheck(_tableName, resolver.GetService<IAmazonDynamoDB>());
    }
}
