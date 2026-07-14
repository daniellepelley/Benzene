using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.OrdersService.Model;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.Mesh.OrdersService.Handlers;

[HttpEndpoint("GET", "/orders")]
[Message("orders:get-all")]
public class GetOrdersMessageHandler : IMessageHandler<Void, OrderDto[]>
{
    private static readonly OrderDto[] Orders =
    {
        new("ord-1", "Espresso Machine", 1),
        new("ord-2", "Coffee Beans (1kg)", 3),
    };

    public Task<IBenzeneResult<OrderDto[]>> HandleAsync(Void request)
    {
        return BenzeneResult.Ok(Orders).AsTask();
    }
}
