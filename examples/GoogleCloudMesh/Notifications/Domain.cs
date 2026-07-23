using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.GoogleCloudMesh.Notifications;

// Inbound event payloads (shapes mirror what the producers publish).
public record OrderPlaced(string OrderId, decimal Amount);
public record PaymentCaptured(string OrderId, string PaymentId, decimal Amount);
public record ShipmentDispatched(string OrderId, string ShipmentId);

/// <summary>notifications-api: a pure consumer that fans in three Pub/Sub event types and logs them.</summary>
[Message("order:placed")]
public class OrderPlacedHandler : IMessageHandler<OrderPlaced, Void>
{
    private readonly ILogger<OrderPlacedHandler> _logger;
    public OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) => _logger = logger;

    public Task<IBenzeneResult<Void>> HandleAsync(OrderPlaced request)
    {
        _logger.LogInformation("notify: order {orderId} placed", request.OrderId);
        return BenzeneResult.Ok<Void>(null).AsTask();
    }
}

[Message("payment:captured")]
public class PaymentCapturedHandler : IMessageHandler<PaymentCaptured, Void>
{
    private readonly ILogger<PaymentCapturedHandler> _logger;
    public PaymentCapturedHandler(ILogger<PaymentCapturedHandler> logger) => _logger = logger;

    public Task<IBenzeneResult<Void>> HandleAsync(PaymentCaptured request)
    {
        _logger.LogInformation("notify: payment {paymentId} captured for order {orderId}", request.PaymentId, request.OrderId);
        return BenzeneResult.Ok<Void>(null).AsTask();
    }
}

[Message("shipment:dispatched")]
public class ShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched, Void>
{
    private readonly ILogger<ShipmentDispatchedHandler> _logger;
    public ShipmentDispatchedHandler(ILogger<ShipmentDispatchedHandler> logger) => _logger = logger;

    public Task<IBenzeneResult<Void>> HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("notify: shipment {shipmentId} dispatched for order {orderId}", request.ShipmentId, request.OrderId);
        return BenzeneResult.Ok<Void>(null).AsTask();
    }
}
