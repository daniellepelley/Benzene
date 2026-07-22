using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Notifications.Model;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AwsMesh.Notifications.Handlers;

/// <summary>
/// Emails "order received" when an order is placed. Subscribes to <c>order:placed</c> over <b>SNS</b> —
/// the fan-out partner of inventory-api (both get every publish).
/// </summary>
[Message("order:placed")]
public class NotifyOnOrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    private readonly ILogger<NotifyOnOrderPlacedHandler> _logger;

    public NotifyOnOrderPlacedHandler(ILogger<NotifyOnOrderPlacedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderPlaced request)
    {
        _logger.LogInformation("emailed order confirmation for {orderId} ('{item}')", request.OrderId, request.Item);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Emails a payment receipt. Subscribes to <c>payment:captured</c> over <b>EventBridge</b> — the same
/// event analytics-api also consumes (one event → many rule targets).
/// </summary>
[Message("payment:captured")]
public class NotifyOnPaymentCapturedHandler : IMessageHandler<PaymentCaptured>
{
    private readonly ILogger<NotifyOnPaymentCapturedHandler> _logger;

    public NotifyOnPaymentCapturedHandler(ILogger<NotifyOnPaymentCapturedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentCaptured request)
    {
        _logger.LogInformation("emailed payment receipt for {orderId} ({amount} {currency})", request.OrderId, request.Amount, request.Currency);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Emails a dispatch notification with tracking. Subscribes to <c>shipping:dispatched</c> over
/// <b>EventBridge</b>.
/// </summary>
[Message("shipping:dispatched")]
public class NotifyOnShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<NotifyOnShipmentDispatchedHandler> _logger;

    public NotifyOnShipmentDispatchedHandler(ILogger<NotifyOnShipmentDispatchedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("emailed dispatch notice for {orderId} ({carrier} {tracking})", request.OrderId, request.Carrier, request.TrackingNumber);
        return Task.CompletedTask;
    }
}
