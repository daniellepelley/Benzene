using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.AzureFunctionsMesh.Shipping;

public record BookShipmentRequest(string OrderId, string Carrier);
public record ShipmentBooked(string ShipmentId, string Status);

/// <summary>
/// Books a shipment, arriving as <c>shipment:book</c> over Service Bus (or HTTP). Terminal in the
/// command chain; the event fan-out (shipment:dispatched over Event Grid) is layered on separately.
/// </summary>
[Message("shipment:book")]
[HttpEndpoint("POST", "/shipments")]
public class BookShipmentHandler : IMessageHandler<BookShipmentRequest, ShipmentBooked>
{
    public Task<IBenzeneResult<ShipmentBooked>> HandleAsync(BookShipmentRequest request)
        => BenzeneResult.Created(new ShipmentBooked($"ship-{request.OrderId}", "booked")).AsTask();
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
