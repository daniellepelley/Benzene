using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.AzureFunctionsMesh.Service;

// Three slightly different domain models — one per deployed service (orders/payments/shipping),
// selected at startup by the MESH_SERVICE env var. Only the selected domain's handler is registered,
// so each Function App exposes its own domain topic and the mesh's aggregated topic catalog shows
// real cross-service ownership. Mirrors examples/K8sMesh/Service/Domain.cs so the two hosting models
// present an identical service surface to the mesh.

/// <summary>Maps a service name to the domain handler type(s) that service exposes.</summary>
public static class Domain
{
    public static Type[] HandlersFor(string service) => service switch
    {
        "payments" => new[] { typeof(TakePaymentHandler) },
        "shipping" => new[] { typeof(BookShipmentHandler) },
        _ => new[] { typeof(CreateOrderHandler) },
    };
}

public record CreateOrderRequest(string CustomerId, string Sku, int Quantity);
public record OrderCreated(string OrderId, string Status);

[Message("order:create")]
[HttpEndpoint("POST", "/orders")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderCreated>
{
    public Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
        => BenzeneResult.Created(new OrderCreated("order-1", "created")).AsTask();
}

public record TakePaymentRequest(string OrderId, decimal Amount, string Currency);
public record PaymentTaken(string PaymentId, string Status);

[Message("payment:take")]
[HttpEndpoint("POST", "/payments")]
public class TakePaymentHandler : IMessageHandler<TakePaymentRequest, PaymentTaken>
{
    public Task<IBenzeneResult<PaymentTaken>> HandleAsync(TakePaymentRequest request)
        => BenzeneResult.Created(new PaymentTaken("pay-1", "captured")).AsTask();
}

public record BookShipmentRequest(string OrderId, string Address, string Carrier);
public record ShipmentBooked(string ShipmentId, string Status);

[Message("shipment:book")]
[HttpEndpoint("POST", "/shipments")]
public class BookShipmentHandler : IMessageHandler<BookShipmentRequest, ShipmentBooked>
{
    public Task<IBenzeneResult<ShipmentBooked>> HandleAsync(BookShipmentRequest request)
        => BenzeneResult.Created(new ShipmentBooked("ship-1", "booked")).AsTask();
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
