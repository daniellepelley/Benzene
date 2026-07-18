using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Orders.Model;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AwsMesh.Orders.Handlers;

/// <summary>Lists all orders.</summary>
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
        => BenzeneResult.Ok(Orders).AsTask();
}

/// <summary>Places a new order.</summary>
[HttpEndpoint("POST", "/orders")]
[Message("orders:create")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrder, OrderDto>
{
    public Task<IBenzeneResult<OrderDto>> HandleAsync(CreateOrder request)
        => BenzeneResult.Ok(new OrderDto($"ord-{request.Item.GetHashCode():x}", request.Item, request.Quantity)).AsTask();
}

/// <summary>The request to create an order.</summary>
public class CreateOrder
{
    public string Item { get; set; } = "";
    public int Quantity { get; set; }
}
