using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Shipping.Model;
using Benzene.Http;
using Benzene.Results;
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

/// <summary>Books a shipment for an order.</summary>
[HttpEndpoint("POST", "/shipments")]
[Message("shipping:book")]
public class BookShipmentMessageHandler : IMessageHandler<BookShipment, ShipmentDto>
{
    public Task<IBenzeneResult<ShipmentDto>> HandleAsync(BookShipment request)
        => BenzeneResult.Ok(new ShipmentDto($"shp-{request.OrderId}", request.OrderId, request.Carrier, $"{request.Carrier}-NEW", "booked")).AsTask();
}

/// <summary>The request to book a shipment.</summary>
public class BookShipment
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
}
