using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.ShippingService.Model;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.Mesh.ShippingService.Handlers;

[HttpEndpoint("GET", "/shipments/{id}")]
[Message("shipments:get")]
public class GetShipmentMessageHandler : IMessageHandler<GetShipmentMessage, ShipmentDto>
{
    public Task<IBenzeneResult<ShipmentDto>> HandleAsync(GetShipmentMessage request)
    {
        return BenzeneResult.Ok(new ShipmentDto(request.Id, "Speedy Freight", "InTransit")).AsTask();
    }
}
