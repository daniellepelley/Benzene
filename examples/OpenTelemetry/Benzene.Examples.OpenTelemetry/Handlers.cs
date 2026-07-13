using System.Diagnostics;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Examples.OpenTelemetry;

public class GreetingRequest
{
    public string Name { get; set; } = "world";
}

public class GreetingResponse
{
    public string Message { get; set; } = "";
}

public class CreateOrderRequest
{
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; } = 1;
}

public class OrderReceipt
{
    public string OrderId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}

public class FailOrderRequest
{
    public string Reason { get; set; } = "card-declined";
}

public interface IWarehouseService
{
    Task<string> DispatchAsync(string productId, int quantity);
}

public class WarehouseService : IWarehouseService
{
    public async Task<string> DispatchAsync(string productId, int quantity)
    {
        using (var reserve = ExampleDiagnostics.ActivitySource.StartActivity("Warehouse.ReserveStock"))
        {
            reserve?.SetTag("order.product_id", productId);
            reserve?.SetTag("order.quantity", quantity);
            await Task.Delay(Random.Shared.Next(30, 80));
        }

        using (var dispatch = ExampleDiagnostics.ActivitySource.StartActivity("Warehouse.Dispatch"))
        {
            await Task.Delay(Random.Shared.Next(50, 120));
            var trackingId = Guid.NewGuid().ToString("N")[..8];
            dispatch?.SetTag("order.tracking_id", trackingId);
            return trackingId;
        }
    }
}

[Message("greeting")]
public class GreetingMessageHandler : IMessageHandler<GreetingRequest, GreetingResponse>
{
    public async Task<IBenzeneResult<GreetingResponse>> HandleAsync(GreetingRequest request)
    {
        await Task.Delay(Random.Shared.Next(5, 25));
        return BenzeneResult.Ok(new GreetingResponse { Message = $"Hello, {request.Name}!" });
    }
}

[Message("order_create")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderRequest, OrderReceipt>
{
    private readonly IWarehouseService _warehouse;

    public CreateOrderMessageHandler(IWarehouseService warehouse)
    {
        _warehouse = warehouse;
    }

    public async Task<IBenzeneResult<OrderReceipt>> HandleAsync(CreateOrderRequest request)
    {
        using (var payment = ExampleDiagnostics.ActivitySource.StartActivity("Payment.Charge"))
        {
            payment?.SetTag("payment.amount", 9.99m * request.Quantity);
            await Task.Delay(Random.Shared.Next(40, 100));
        }

        await _warehouse.DispatchAsync(request.ProductId, request.Quantity);

        return BenzeneResult.Created(new OrderReceipt
        {
            OrderId = Guid.NewGuid().ToString("N")[..8],
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Total = 9.99m * request.Quantity
        });
    }
}

[Message("order_fail")]
public class FailingOrderMessageHandler : IMessageHandler<FailOrderRequest, OrderReceipt>
{
    public async Task<IBenzeneResult<OrderReceipt>> HandleAsync(FailOrderRequest request)
    {
        using var payment = ExampleDiagnostics.ActivitySource.StartActivity("Payment.Charge");
        await Task.Delay(Random.Shared.Next(20, 60));

        var ex = new InvalidOperationException($"Payment gateway rejected the order: {request.Reason}");
        payment?.SetStatus(ActivityStatusCode.Error, ex.Message);
        payment?.AddException(ex);
        throw ex;
    }
}
