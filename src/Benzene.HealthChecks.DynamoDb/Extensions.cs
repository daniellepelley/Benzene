using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.DynamoDb;

/// <summary>Registration helper for <see cref="DynamoDbHealthCheck"/>.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <see cref="DynamoDbHealthCheck"/> for <paramref name="tableName"/>, resolving
    /// <c>IAmazonDynamoDB</c> from DI (the consumer must register it).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="tableName">The table to check.</param>
    public static IHealthCheckBuilder AddDynamoDbHealthCheck(this IHealthCheckBuilder builder, string tableName)
    {
        return builder.AddHealthCheckFactory(new DynamoDbHealthCheckFactory(tableName));
    }
}
