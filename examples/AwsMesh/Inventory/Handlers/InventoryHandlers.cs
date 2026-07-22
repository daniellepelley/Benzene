using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Inventory.Model;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AwsMesh.Inventory.Handlers;

/// <summary>
/// Reserves stock when an order is placed. Subscribes to <c>order:placed</c> — which arrives over
/// <b>SNS</b>, fanned out from orders-api to this service and notifications-api at once. A pure event
/// consumer: <see cref="IMessageHandler{TRequest}"/> produces no response.
/// </summary>
[Message("order:placed")]
public class ReserveStockOnOrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    private readonly ILogger<ReserveStockOnOrderPlacedHandler> _logger;

    public ReserveStockOnOrderPlacedHandler(ILogger<ReserveStockOnOrderPlacedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderPlaced request)
    {
        _logger.LogInformation("reserved {quantity}x '{item}' for order {orderId}", request.Quantity, request.Item, request.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Turns a reservation into a decrement once the shipment leaves. Subscribes to
/// <c>shipping:dispatched</c> — which arrives over <b>EventBridge</b>, routed here by rule. So this one
/// service consumes from two different event transports.
/// </summary>
[Message("shipping:dispatched")]
public class DecrementStockOnShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<DecrementStockOnShipmentDispatchedHandler> _logger;

    public DecrementStockOnShipmentDispatchedHandler(ILogger<DecrementStockOnShipmentDispatchedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("decremented stock for order {orderId} (shipment {shipmentId} dispatched)", request.OrderId, request.ShipmentId);
        return Task.CompletedTask;
    }
}
