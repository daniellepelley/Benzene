using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AzureFunctionsMesh.Payments;

public record TakePaymentRequest(string OrderId, decimal Amount, string Currency);
public record PaymentTaken(string PaymentId, string Status);

/// <summary>
/// The command payments-api sends to shipping-api over a <b>Service Bus queue</b> (topic
/// <c>shipment:book</c>) once payment is taken. Declared in the spec's <c>events</c> → structural edge
/// payments → shipping.
/// </summary>
public record OutboundBookShipment(string OrderId, string Carrier);

/// <summary>
/// Takes payment (arriving as <c>payment:take</c> over Service Bus or HTTP), then commands shipping-api
/// to book over Service Bus. Best-effort onward send.
/// </summary>
[Message("payment:take")]
[HttpEndpoint("POST", "/payments")]
public class TakePaymentHandler : IMessageHandler<TakePaymentRequest, PaymentTaken>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<TakePaymentHandler> _logger;

    public TakePaymentHandler(IBenzeneMessageSender sender, ILogger<TakePaymentHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<PaymentTaken>> HandleAsync(TakePaymentRequest request)
    {
        var payment = new PaymentTaken($"pay-{request.OrderId}", "captured");

        try
        {
            await _sender.SendAsync<OutboundBookShipment, Void>("shipment:book",
                new OutboundBookShipment(request.OrderId, "DPD"));
            _logger.LogInformation("payment taken for {orderId}; sent shipment:book", request.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "downstream shipment:book send failed for {orderId}", request.OrderId);
        }

        return BenzeneResult.Created(payment);
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
