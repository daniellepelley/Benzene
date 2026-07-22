using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using V1 = Benzene.Examples.K8sMesh.Service.Model.V1;
using V2 = Benzene.Examples.K8sMesh.Service.Model.V2;

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
        // in-cluster Kubernetes DNS name (DOWNSTREAM_MSG_URL). orders-api is pinned to payment:take VERSION 1
        // (no currency) - it sends the v1 payload declaring version "1", which travels in the benzene-version
        // envelope header; payments-api's single v2 handler never sees v1, the caster upcasts it first
        // (docs/specification/versioning.md). Best-effort - HttpBenzeneMessageClient never throws.
        var payment = new V1.TakePaymentRequest { OrderId = order.OrderId, Amount = request.Quantity * 10m };
        var result = await _downstream.SendMessageAsync<V1.TakePaymentRequest, PaymentTaken>("payment:take", payment, version: "1");
        _logger.LogInformation("order {orderId} created; payment:take (v1) -> {status}", order.OrderId, result.Status);

        return BenzeneResult.Created(order);
    }
}

public record PaymentTaken(string PaymentId, string Status);

// The SINGLE payment:take handler, written against version 2 only ([Message("payment:take", "2")], taking
// V2.TakePaymentRequest). A v1 payload from orders-api is upcast to v2 before this runs (Startup wires the
// V1->V2 caster + UsePayloadVersionCasting for the payments service), so request.Currency is always present -
// seeded by the upcast when the producer was on v1. This is the "send v1 -> upcast -> one v2 handler" story.
[Message("payment:take", "2")]
[HttpEndpoint("POST", "/payments")]
public class TakePaymentHandler : IMessageHandler<V2.TakePaymentRequest, PaymentTaken>
{
    private readonly IBenzeneMessageClient _downstream;
    private readonly ILogger<TakePaymentHandler> _logger;

    public TakePaymentHandler(IBenzeneMessageClient downstream, ILogger<TakePaymentHandler> logger)
    {
        _downstream = downstream;
        _logger = logger;
    }

    public async Task<IBenzeneResult<PaymentTaken>> HandleAsync(V2.TakePaymentRequest request)
    {
        var payment = new PaymentTaken("pay-1", "captured");
        // Currency is always set even for a v1 producer - proof the upcast ran (v1 carried no currency).
        _logger.LogInformation("payment {paymentId} captured in {currency} (v2 handler)", payment.PaymentId, request.Currency);

        // Second hop: once captured, ask the shipping service to book the shipment.
        var shipment = new BookShipmentRequest(request.OrderId, "123 Example St", "royal-mail");
        var result = await _downstream.SendMessageAsync<BookShipmentRequest, ShipmentBooked>("shipment:book", shipment);
        _logger.LogInformation("shipment:book -> {status}", result.Status);

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
