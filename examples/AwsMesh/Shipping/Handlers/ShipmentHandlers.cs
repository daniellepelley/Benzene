using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Shipping.Model;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AwsMesh.Shipping.Handlers;

/// <summary>Lists all shipments.</summary>
[HttpEndpoint("GET", "/shipments")]
[Message("shipping:get-all")]
public class GetShipmentsMessageHandler : IMessageHandler<Void, ShipmentDto[]>
{
    private static readonly ShipmentDto[] Shipments =
    {
        new("shp-1", "ord-1", "DPD", "DPD-99811", "in-transit"),
        new("shp-2", "ord-2", "RoyalMail", "RM-24501", "delivered"),
    };

    public Task<IBenzeneResult<ShipmentDto[]>> HandleAsync(Void request)
        => BenzeneResult.Ok(Shipments).AsTask();
}

/// <summary>
/// Books a shipment for an order, then publishes <c>shipping:dispatched</c> to EventBridge — the
/// terminal event, routed to inventory/notifications/analytics. Best-effort, same posture as the
/// upstream sends.
/// </summary>
[HttpEndpoint("POST", "/shipments")]
[Message("shipping:book")]
public class BookShipmentMessageHandler : IMessageHandler<BookShipment, ShipmentDto>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<BookShipmentMessageHandler> _logger;

    public BookShipmentMessageHandler(IBenzeneMessageSender sender, ILogger<BookShipmentMessageHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<ShipmentDto>> HandleAsync(BookShipment request)
    {
        var shipment = new ShipmentDto($"shp-{request.OrderId}", request.OrderId, request.Carrier, $"{request.Carrier}-NEW", "booked");

        try
        {
            await _sender.SendAsync<OutboundShipmentDispatched, Void>("shipping:dispatched",
                new OutboundShipmentDispatched { OrderId = request.OrderId, ShipmentId = shipment.Id, Carrier = shipment.Carrier, TrackingNumber = shipment.TrackingNumber });
            _logger.LogInformation("shipment booked for {orderId}; published shipping:dispatched", request.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "shipping:dispatched publish failed for {orderId}", request.OrderId);
        }

        return BenzeneResult.Ok(shipment);
    }
}

/// <summary>The request to book a shipment.</summary>
public class BookShipment
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
}
