using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.K8sMesh.Service;

// Three slightly different domain models — one per deployed service (orders/payments/shipping),
// selected at startup by the MESH_SERVICE env var. Only the selected domain's handler is registered,
// so each service exposes its own domain topic (and the mesh's aggregated topic catalog shows real
// cross-service ownership).
//
// The services also CHAIN to each other over HTTP: orders → payments → shipping, each hop carried as a
// lightweight BenzeneMessage envelope POSTed to the next service's /benzene-message endpoint via
// HttpBenzeneMessageClient (Startup wires it from the DOWNSTREAM_MSG_URL env var). This turns the mesh
// from a discovery-only demo into one with real service-to-service traffic to observe.

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
    private readonly IBenzeneMessageClient _downstream;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(IBenzeneMessageClient downstream, ILogger<CreateOrderHandler> logger)
    {
        _downstream = downstream;
        _logger = logger;
    }

    public async Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
    {
        var order = new OrderCreated("order-1", "created");

        // Chain the next hop over HTTP: ask the payments service to take payment, addressing it by its
        // in-cluster Kubernetes DNS name (DOWNSTREAM_MSG_URL). Best-effort — HttpBenzeneMessageClient never
        // throws (a transport error maps to ServiceUnavailable), and with no downstream wired the null client
        // just returns Accepted, so order creation is never blocked by a downstream blip.
        var payment = new TakePaymentRequest(order.OrderId, request.Quantity * 10m, "GBP");
        var result = await _downstream.SendMessageAsync<TakePaymentRequest, PaymentTaken>("payment:take", payment);
        _logger.LogInformation("order {orderId} created; payment:take -> {status}", order.OrderId, result.Status);

        return BenzeneResult.Created(order);
    }
}

public record TakePaymentRequest(string OrderId, decimal Amount, string Currency);
public record PaymentTaken(string PaymentId, string Status);

[Message("payment:take")]
[HttpEndpoint("POST", "/payments")]
public class TakePaymentHandler : IMessageHandler<TakePaymentRequest, PaymentTaken>
{
    private readonly IBenzeneMessageClient _downstream;
    private readonly ILogger<TakePaymentHandler> _logger;

    public TakePaymentHandler(IBenzeneMessageClient downstream, ILogger<TakePaymentHandler> logger)
    {
        _downstream = downstream;
        _logger = logger;
    }

    public async Task<IBenzeneResult<PaymentTaken>> HandleAsync(TakePaymentRequest request)
    {
        var payment = new PaymentTaken("pay-1", "captured");

        // Second hop: once captured, ask the shipping service to book the shipment.
        var shipment = new BookShipmentRequest(request.OrderId, "123 Example St", "royal-mail");
        var result = await _downstream.SendMessageAsync<BookShipmentRequest, ShipmentBooked>("shipment:book", shipment);
        _logger.LogInformation("payment {paymentId} captured; shipment:book -> {status}", payment.PaymentId, result.Status);

        return BenzeneResult.Created(payment);
    }
}

public record BookShipmentRequest(string OrderId, string Address, string Carrier);
public record ShipmentBooked(string ShipmentId, string Status);

[Message("shipment:book")]
[HttpEndpoint("POST", "/shipments")]
public class BookShipmentHandler : IMessageHandler<BookShipmentRequest, ShipmentBooked>
{
    // Terminal service — books the shipment and returns; no further downstream, so no client is injected.
    public Task<IBenzeneResult<ShipmentBooked>> HandleAsync(BookShipmentRequest request)
        => BenzeneResult.Created(new ShipmentBooked("ship-1", "booked")).AsTask();
}

/// <summary>
/// Stand-in <see cref="IBenzeneMessageClient"/> for a service with no downstream wired
/// (<c>DOWNSTREAM_MSG_URL</c> unset — e.g. a standalone single-service run). Accepts every send as a no-op
/// so the chaining handlers still resolve and run unchanged without a real next hop.
/// </summary>
public class NullBenzeneMessageClient : IBenzeneMessageClient
{
    public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        => Task.FromResult(BenzeneResult.Accepted<TResponse>());

    public void Dispose()
    {
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
