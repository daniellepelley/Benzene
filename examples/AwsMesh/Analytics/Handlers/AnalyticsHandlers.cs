using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Analytics.Model;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AwsMesh.Analytics.Handlers;

/// <summary>
/// Records revenue when a payment settles. Subscribes to <c>payment:captured</c> over <b>EventBridge</b>
/// — the same event notifications-api also consumes, proving one event fans out to multiple rule targets.
/// </summary>
[Message("payment:captured")]
public class RecordPaymentCapturedHandler : IMessageHandler<PaymentCaptured>
{
    private readonly ILogger<RecordPaymentCapturedHandler> _logger;

    public RecordPaymentCapturedHandler(ILogger<RecordPaymentCapturedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentCaptured request)
    {
        _logger.LogInformation("recorded revenue metric: {amount} {currency} for {orderId}", request.Amount, request.Currency, request.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Records a fulfilment metric when a shipment leaves. Subscribes to <c>shipping:dispatched</c> over
/// <b>EventBridge</b>.
/// </summary>
[Message("shipping:dispatched")]
public class RecordShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<RecordShipmentDispatchedHandler> _logger;

    public RecordShipmentDispatchedHandler(ILogger<RecordShipmentDispatchedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("recorded fulfilment metric for {orderId} ({carrier})", request.OrderId, request.Carrier);
        return Task.CompletedTask;
    }
}
