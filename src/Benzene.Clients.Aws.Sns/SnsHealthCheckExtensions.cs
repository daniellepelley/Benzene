using Amazon.SimpleNotificationService;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sns;

/// <summary>Registration helper for <see cref="SnsHealthCheck"/>.</summary>
public static class SnsHealthCheckExtensions
{
    /// <summary>
    /// Registers an <see cref="SnsHealthCheck"/> for <paramref name="topicArn"/>, resolving
    /// <see cref="IAmazonSimpleNotificationService"/> from DI (the consumer must register it).
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="topicArn">The ARN of the topic to check.</param>
    public static IHealthCheckBuilder AddSnsHealthCheck(this IHealthCheckBuilder builder, string topicArn)
    {
        return builder.AddHealthCheck(resolver =>
            new SnsHealthCheck(topicArn, resolver.GetService<IAmazonSimpleNotificationService>()));
    }
}
