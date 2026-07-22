using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.AzureFunctionsMesh.Inventory;

/// <summary>The <c>order:placed</c> event as inventory-api consumes it (over Event Hub).</summary>
public record OrderPlaced(string OrderId, string Sku, int Quantity);

/// <summary>The <c>shipment:dispatched</c> event as inventory-api consumes it (over Event Grid).</summary>
public record ShipmentDispatched(string OrderId, string ShipmentId);

/// <summary>
/// Reserves stock when an order is placed — subscribes to <c>order:placed</c> over <b>Event Hub</b>
/// (inventory's own consumer group; notifications reads the same hub on its own group). A pure event
/// consumer: <see cref="IMessageHandler{TRequest}"/> produces no response.
/// </summary>
[Message("order:placed")]
public class ReserveStockHandler : IMessageHandler<OrderPlaced>
{
    private readonly ILogger<ReserveStockHandler> _logger;
    public ReserveStockHandler(ILogger<ReserveStockHandler> logger) => _logger = logger;

    public Task HandleAsync(OrderPlaced request)
    {
        _logger.LogInformation("reserved {quantity}x '{sku}' for order {orderId}", request.Quantity, request.Sku, request.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Decrements stock once a shipment leaves — subscribes to <c>shipment:dispatched</c> over <b>Event
/// Grid</b>. So inventory-api consumes from two different event transports.
/// </summary>
[Message("shipment:dispatched")]
public class DecrementStockHandler : IMessageHandler<ShipmentDispatched>
{
    private readonly ILogger<DecrementStockHandler> _logger;
    public DecrementStockHandler(ILogger<DecrementStockHandler> logger) => _logger = logger;

    public Task HandleAsync(ShipmentDispatched request)
    {
        _logger.LogInformation("decremented stock for order {orderId} (shipment {shipmentId})", request.OrderId, request.ShipmentId);
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
