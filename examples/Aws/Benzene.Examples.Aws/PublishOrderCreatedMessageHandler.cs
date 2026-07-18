using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Http;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.Aws;

/// <summary>
/// Egress demo (release plan Tier 2.3): the ingress↔egress symmetry, made copy-runnable. Publishes
/// the incoming <see cref="OrderCreatedEvent"/> onto the same SQS queue the SQS ingress trigger
/// consumes from (see <see cref="DependenciesBuilder"/>'s <c>AddOutboundRouting</c> wiring), via
/// <see cref="IBenzeneMessageSender"/> - the same transport-agnostic port a generated client uses.
/// Reachable like any other handler (HTTP here) to keep the demo self-contained; in a real service
/// this call would typically live at the end of <c>CreateOrderMessageHandler</c> or similar, right
/// after the order is actually created.
/// </summary>
[HttpEndpoint("POST", "/orders/publish-created")]
[Message("order_publish_demo")]
public class PublishOrderCreatedMessageHandler : IMessageHandler<OrderCreatedEvent, Void>
{
    private readonly IBenzeneMessageSender _sender;

    public PublishOrderCreatedMessageHandler(IBenzeneMessageSender sender)
    {
        _sender = sender;
    }

    public Task<IBenzeneResult<Void>> HandleAsync(OrderCreatedEvent request)
    {
        return _sender.SendAsync<OrderCreatedEvent, Void>(MessageTopicNames.OrderCreated, request);
    }
}
