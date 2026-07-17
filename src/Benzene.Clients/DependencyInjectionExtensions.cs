using Benzene.Abstractions.DI;

namespace Benzene.Clients;

/// <summary>
/// Top-level DI registration for the outbound routing mechanism.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Builds the topic-keyed outbound routing table and registers the resulting
    /// <see cref="IBenzeneMessageSender"/>.
    /// </summary>
    /// <param name="services">The service container to register into.</param>
    /// <param name="configure">Registers each topic's route via <see cref="OutboundRoutingBuilder.Route"/>.</param>
    /// <returns>The same container, for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddOutboundRouting(routing =&gt; routing
    ///     .Route("order:create", pipeline =&gt; pipeline.UseSqs(queueUrl).UseRetry(3))
    ///     .Route("audit:log", pipeline =&gt; pipeline.UseSns(topicArn)));
    /// </code>
    /// </example>
    public static IBenzeneServiceContainer AddOutboundRouting(this IBenzeneServiceContainer services, Action<OutboundRoutingBuilder> configure)
    {
        var builder = new OutboundRoutingBuilder(services);
        configure(builder);
        var routes = builder.Build();

        services.AddScoped<IBenzeneMessageSender>(resolver => new DefaultBenzeneMessageSender(routes, resolver));

        return services;
    }
}
