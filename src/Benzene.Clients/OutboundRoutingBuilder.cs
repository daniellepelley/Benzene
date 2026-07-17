using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Clients;

/// <summary>
/// Builds the topic-keyed outbound routing table: one <see cref="IMiddlewarePipeline{TContext}"/>
/// per topic, replacing <c>ClientsBuilder</c>/<c>SingleClientsBuilder</c>'s split-by-cardinality
/// shape outright - "one client" is just the N=1 case of "many". Registered via
/// <c>AddOutboundRouting(...)</c>; see <c>work/benzene-clients-redesign-plan.md</c> §2.2.
/// </summary>
public class OutboundRoutingBuilder
{
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;
    private readonly List<(string Topic, IMiddlewarePipelineBuilder<OutboundContext> Builder)> _routes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundRoutingBuilder"/> class.
    /// </summary>
    /// <param name="benzeneServiceContainer">The service container each route's pipeline builder registers dependencies into.</param>
    public OutboundRoutingBuilder(IBenzeneServiceContainer benzeneServiceContainer)
    {
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    /// <summary>
    /// Registers an outbound pipeline for <paramref name="topic"/>.
    /// </summary>
    /// <param name="topic">The topic this pipeline sends messages for.</param>
    /// <param name="configure">Builds the pipeline - e.g. transport middleware plus cross-cutting
    /// concerns like retry/correlation/trace-context.</param>
    /// <returns>This builder, for chaining.</returns>
    public OutboundRoutingBuilder Route(string topic, Action<IMiddlewarePipelineBuilder<OutboundContext>> configure)
    {
        var builder = new MiddlewarePipelineBuilder<OutboundContext>(_benzeneServiceContainer);
        configure(builder);
        _routes.Add((topic, builder));
        return this;
    }

    /// <summary>
    /// Builds the final topic-keyed routing table.
    /// </summary>
    /// <returns>Each registered topic's built pipeline.</returns>
    /// <exception cref="DuplicateOutboundRouteException">The same topic was registered more than once.</exception>
    public IReadOnlyDictionary<string, IMiddlewarePipeline<OutboundContext>> Build()
    {
        var duplicate = _routes
            .GroupBy(x => x.Topic)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new DuplicateOutboundRouteException(duplicate.Key);
        }

        return _routes.ToDictionary(x => x.Topic, x => x.Builder.Build());
    }
}
