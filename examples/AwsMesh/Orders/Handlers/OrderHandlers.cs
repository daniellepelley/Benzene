using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Orders.Model;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
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

/// <summary>
/// Places a new order and chains the next hop — asks payments-api to capture (topic
/// <c>payments:capture</c>, routed to its SQS ingress). Best-effort: a downstream hiccup never fails
/// the order, and with no queue wired (e.g. the Lambda test tool) it just logs and returns.
/// </summary>
[HttpEndpoint("POST", "/orders")]
[Message("orders:create")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrder, OrderDto>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<CreateOrderMessageHandler> _logger;

    public CreateOrderMessageHandler(IBenzeneMessageSender sender, ILogger<CreateOrderMessageHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<OrderDto>> HandleAsync(CreateOrder request)
    {
        var order = new OrderDto($"ord-{request.Item.GetHashCode():x}", request.Item, request.Quantity);
        var amount = request.Quantity * 10m;

        // Point-to-point command over SQS: exactly one consumer (payments-api) must capture this.
        try
        {
            await _sender.SendAsync<OutboundPaymentCapture, Void>("payments:capture",
                new OutboundPaymentCapture { OrderId = order.Id, Amount = amount, Currency = "GBP" });
            _logger.LogInformation("order {orderId} created; sent payments:capture", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "downstream payments:capture send failed for {orderId}", order.Id);
        }

        // Fan-out event over SNS: every subscriber (inventory-api, notifications-api) gets it. Separate
        // from the command above — the order is placed regardless of who's listening.
        try
        {
            await _sender.SendAsync<OutboundOrderPlaced, Void>("order:placed",
                new OutboundOrderPlaced { OrderId = order.Id, Item = order.Item, Quantity = order.Quantity, Amount = amount, Currency = "GBP" });
            _logger.LogInformation("order {orderId} created; published order:placed", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "order:placed publish failed for {orderId}", order.Id);
        }

        return BenzeneResult.Created(order);
    }
}

/// <summary>The request to create an order.</summary>
public class CreateOrder
{
    public string Item { get; set; } = "";
    public int Quantity { get; set; }
}
