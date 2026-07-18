using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Payments.Model;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AwsMesh.Payments.Handlers;

/// <summary>Lists all payments.</summary>
[HttpEndpoint("GET", "/payments")]
[Message("payments:get-all")]
public class GetPaymentsMessageHandler : IMessageHandler<Void, PaymentDto[]>
{
    private static readonly PaymentDto[] Payments =
    {
        new("pay-1", "ord-1", 549.00m, "GBP", "captured"),
        new("pay-2", "ord-2", 24.00m, "GBP", "authorized"),
    };

    public Task<IBenzeneResult<PaymentDto[]>> HandleAsync(Void request)
        => BenzeneResult.Ok(Payments).AsTask();
}

/// <summary>
/// Captures a payment for an order and chains the final hop — asks shipping-api to book (topic
/// <c>shipping:book</c>, routed to its SQS ingress). Best-effort, same posture as orders-api's send.
/// </summary>
[HttpEndpoint("POST", "/payments")]
[Message("payments:capture")]
public class CapturePaymentMessageHandler : IMessageHandler<CapturePayment, PaymentDto>
{
    private readonly IBenzeneMessageSender _sender;
    private readonly ILogger<CapturePaymentMessageHandler> _logger;

    public CapturePaymentMessageHandler(IBenzeneMessageSender sender, ILogger<CapturePaymentMessageHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<IBenzeneResult<PaymentDto>> HandleAsync(CapturePayment request)
    {
        var payment = new PaymentDto($"pay-{request.OrderId}", request.OrderId, request.Amount, request.Currency, "captured");

        try
        {
            await _sender.SendAsync<OutboundShipmentBook, Void>("shipping:book",
                new OutboundShipmentBook { OrderId = request.OrderId, Carrier = "DPD" });
            _logger.LogInformation("payment captured for {orderId}; sent shipping:book", request.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "downstream shipping:book send failed for {orderId}", request.OrderId);
        }

        return BenzeneResult.Created(payment);
    }
}

/// <summary>The request to capture a payment.</summary>
public class CapturePayment
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}
