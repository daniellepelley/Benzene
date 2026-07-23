using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.GoogleCloudMesh.Orders;

public record CreateOrderRequest(string OrderId, decimal Amount);
public record OrderCreated(string OrderId, string Status);

/// <summary>Sent to payments (Benzene topic <c>payment:take</c>).</summary>
public record OutboundTakePayment(string OrderId, decimal Amount);
/// <summary>Broadcast (Benzene topic <c>order:placed</c>) to interested consumers (e.g. notifications).</summary>
public record OutboundOrderPlaced(string OrderId, decimal Amount);

/// <summary>
/// Accepts an order over HTTP, then fans out two Pub/Sub events — a point-to-point command to payments
/// and a broadcast that the order was placed. Both go out via <see cref="IBenzeneMessageSender"/>; the
/// handler is transport-agnostic.
/// </summary>
[Message("order:create")]
[HttpEndpoint("POST", "/orders")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderCreated>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(IBenzeneMessageSender sender, ILogger<CreateOrderHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
    {
        try { await _sender.SendAsync<OutboundTakePayment, Void>("payment:take", new OutboundTakePayment(request.OrderId, request.Amount)); }
        catch (Exception ex) { _logger.LogWarning(ex, "publish payment:take failed for {orderId}", request.OrderId); }

        try { await _sender.SendAsync<OutboundOrderPlaced, Void>("order:placed", new OutboundOrderPlaced(request.OrderId, request.Amount)); }
        catch (Exception ex) { _logger.LogWarning(ex, "publish order:placed failed for {orderId}", request.OrderId); }

        return BenzeneResult.Created(new OrderCreated(request.OrderId, "created"));
    }
}
