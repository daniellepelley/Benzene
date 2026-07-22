using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AzureFunctionsMesh.Orders;

public record CreateOrderRequest(string CustomerId, string Sku, int Quantity);
public record OrderCreated(string OrderId, string Status);

/// <summary>
/// The command orders-api sends to payments-api over a <b>Service Bus queue</b> (topic
/// <c>payment:take</c>) — a point-to-point command, exactly one consumer. Declaring it (see StartUp)
/// puts it in the spec's <c>events</c> → the mesh's structural edge orders → payments.
/// </summary>
public record OutboundTakePayment(string OrderId, decimal Amount, string Currency);

/// <summary>Places an order, then commands payments-api to take payment over Service Bus.</summary>
[Message("order:create")]
[HttpEndpoint("POST", "/orders")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderCreated>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(IBenzeneMessageSender sender, ILogger<CreateOrderHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
    {
        var order = new OrderCreated($"order-{request.Sku}", "created");

        // Point-to-point command over Service Bus: payments-api must take this exactly once.
        try
        {
            await _sender.SendAsync<OutboundTakePayment, Void>("payment:take",
                new OutboundTakePayment(order.OrderId, request.Quantity * 10m, "GBP"));
            _logger.LogInformation("order {orderId} created; sent payment:take", order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "downstream payment:take send failed for {orderId}", order.OrderId);
        }

        return BenzeneResult.Created(order);
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
