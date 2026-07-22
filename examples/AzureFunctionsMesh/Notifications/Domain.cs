using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AzureFunctionsMesh.Notifications;

public record OrderPlaced(string OrderId, string Sku);
public record PaymentCaptured(string OrderId, decimal Amount, string Currency);
public record ShipmentDispatched(string OrderId, string Carrier);

/// <summary>Emails "order received" — subscribes to <c>order:placed</c> over <b>Event Hub</b> (fan-out with inventory).</summary>
[Message("order:placed")]
public class NotifyOnOrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    private readonly ILogger<NotifyOnOrderPlacedHandler> _logger;
    public NotifyOnOrderPlacedHandler(ILogger<NotifyOnOrderPlacedHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlaced request)
    {
        _logger.LogInformation("emailed order confirmation for {orderId} ('{sku}')", request.OrderId, request.Sku);
        return Task.CompletedTask;
    }
}

/// <summary>Emails a receipt — subscribes to <c>payment:captured</c> over <b>Event Grid</b> (shared with analytics).</summary>
[Message("payment:captured")]
public class NotifyOnPaymentCapturedHandler : IMessageHandler<PaymentCaptured>
{
    private readonly ILogger<NotifyOnPaymentCapturedHandler> _logger;
    public NotifyOnPaymentCapturedHandler(ILogger<NotifyOnPaymentCapturedHandler> logger) => _logger = logger;

    public Task HandleAsync(PaymentCaptured request)
    {
        _logger.LogInformation("emailed payment receipt for {orderId} ({amount} {currency})", request.OrderId, request.Amount, request.Currency);
        return Task.CompletedTask;
    }
}

/// <summary>Emails dispatch notice — subscribes to <c>shipment:dispatched</c> over <b>Event Grid</b>.</summary>
[Message("shipment:dispatched")]
public class NotifyOnShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<NotifyOnShipmentDispatchedHandler> _logger;
    public NotifyOnShipmentDispatchedHandler(ILogger<NotifyOnShipmentDispatchedHandler> logger) => _logger = logger;

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("emailed dispatch notice for {orderId} ({carrier})", request.OrderId, request.Carrier);
        return Task.CompletedTask;
    }
}

/// <summary>A trivial always-healthy check so the service is Cloud Service Profile-conformant.</summary>
public class ServiceHealthCheck : IHealthCheck
{
    private readonly string _service;
    public ServiceHealthCheck(string service) => _service = service;

    public string Type => "self";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var data = new Dictionary<string, object> { ["service"] = _service };
        return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, Array.Empty<HealthCheckDependency>()));
    }
}
