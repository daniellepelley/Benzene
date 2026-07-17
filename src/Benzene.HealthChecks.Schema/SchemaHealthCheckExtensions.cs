using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Schema;

/// <summary>
/// Provides extension methods for adding the provider-side schema/contract health check.
/// </summary>
public static class SchemaHealthCheckExtensions
{
    /// <summary>
    /// Adds a <see cref="SchemaHealthCheck"/> to the health-check pipeline, resolving the handler
    /// lookup from DI when the check runs.
    /// </summary>
    /// <param name="source">The health-check builder.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IHealthCheckBuilder AddSchemaHealthCheck(this IHealthCheckBuilder source)
    {
        return source.AddHealthCheck(resolver =>
            new SchemaHealthCheck(resolver.GetService<IMessageHandlerDefinitionLookUp>()));
    }
}
