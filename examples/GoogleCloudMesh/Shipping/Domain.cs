using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.GoogleCloudMesh.Shipping;

public record BookShipmentRequest(string OrderId, string Carrier);
public record ShipmentBooked(string ShipmentId, string Status);

public record OutboundShipmentDispatched(string OrderId, string ShipmentId);

/// <summary>
/// Books a shipment (arriving over Pub/Sub as <c>shipment:book</c>, or HTTP) and publishes
/// <c>shipment:dispatched</c>.
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
        var shipmentId = $"shp-{request.OrderId}";

        try { await _sender.SendAsync<OutboundShipmentDispatched, Void>("shipment:dispatched", new OutboundShipmentDispatched(request.OrderId, shipmentId)); }
        catch (Exception ex) { _logger.LogWarning(ex, "publish shipment:dispatched failed for {orderId}", request.OrderId); }

        return BenzeneResult.Created(new ShipmentBooked(shipmentId, "booked"));
    }
}
