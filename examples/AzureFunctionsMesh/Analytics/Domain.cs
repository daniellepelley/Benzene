using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AzureFunctionsMesh.Analytics;

public record PaymentCaptured(string OrderId, decimal Amount, string Currency);
public record ShipmentDispatched(string OrderId, string Carrier);

/// <summary>Records revenue — subscribes to <c>payment:captured</c> over <b>Event Grid</b> (shared with notifications).</summary>
[Message("payment:captured")]
public class RecordPaymentCapturedHandler : IMessageHandler<PaymentCaptured>
{
    private readonly ILogger<RecordPaymentCapturedHandler> _logger;
    public RecordPaymentCapturedHandler(ILogger<RecordPaymentCapturedHandler> logger) => _logger = logger;

    public Task HandleAsync(PaymentCaptured request)
    {
        _logger.LogInformation("recorded revenue {amount} {currency} for {orderId}", request.Amount, request.Currency, request.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>Records a fulfilment metric — subscribes to <c>shipment:dispatched</c> over <b>Event Grid</b>.</summary>
[Message("shipment:dispatched")]
public class RecordShipmentDispatchedHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<RecordShipmentDispatchedHandler> _logger;
    public RecordShipmentDispatchedHandler(ILogger<RecordShipmentDispatchedHandler> logger) => _logger = logger;

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("recorded fulfilment metric for {orderId} ({carrier})", request.OrderId, request.Carrier);
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
