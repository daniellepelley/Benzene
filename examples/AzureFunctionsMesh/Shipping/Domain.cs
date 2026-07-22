using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AzureFunctionsMesh.Shipping;

public record BookShipmentRequest(string OrderId, string Carrier);
public record ShipmentBooked(string ShipmentId, string Status);

/// <summary>
/// The terminal integration event shipping-api publishes to <b>Event Grid</b> (topic
/// <c>shipment:dispatched</c>). Routed by subscription to inventory-api, notifications-api and
/// analytics-api. Declared in the spec's <c>events</c> → those structural edges.
/// </summary>
public record OutboundShipmentDispatched(string OrderId, string ShipmentId, string Carrier);

/// <summary>
/// Books a shipment (arriving as <c>shipment:book</c> over Service Bus or HTTP), then publishes
/// <c>shipment:dispatched</c> to Event Grid — the terminal event of the flow.
/// </summary>
[Message("shipment:book")]
[HttpEndpoint("POST", "/shipments")]
public class BookShipmentHandler : IMessageHandler<BookShipmentRequest, ShipmentBooked>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<BookShipmentHandler> _logger;

    public BookShipmentHandler(IBenzeneMessageSender sender, ILogger<BookShipmentHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<ShipmentBooked>> HandleAsync(BookShipmentRequest request)
    {
        var shipment = new ShipmentBooked($"ship-{request.OrderId}", "booked");

        try
        {
            await _sender.SendAsync<OutboundShipmentDispatched, Void>("shipment:dispatched",
                new OutboundShipmentDispatched(request.OrderId, shipment.ShipmentId, request.Carrier));
            _logger.LogInformation("shipment booked for {orderId}; published shipment:dispatched", request.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "shipment:dispatched publish failed for {orderId}", request.OrderId);
        }

        return BenzeneResult.Created(shipment);
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
