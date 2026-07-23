using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.GoogleCloudMesh.Payments;

public record TakePaymentRequest(string OrderId, decimal Amount);
public record PaymentTaken(string PaymentId, string Status);

public record OutboundBookShipment(string OrderId, string Carrier);
public record OutboundPaymentCaptured(string OrderId, string PaymentId, decimal Amount);

/// <summary>
/// Captures a payment (arriving over Pub/Sub as <c>payment:take</c>, or HTTP), then publishes a
/// point-to-point <c>shipment:book</c> command and a <c>payment:captured</c> event.
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
        var paymentId = $"pay-{request.OrderId}";

        try { await _sender.SendAsync<OutboundBookShipment, Void>("shipment:book", new OutboundBookShipment(request.OrderId, "DPD")); }
        catch (Exception ex) { _logger.LogWarning(ex, "publish shipment:book failed for {orderId}", request.OrderId); }

        try { await _sender.SendAsync<OutboundPaymentCaptured, Void>("payment:captured", new OutboundPaymentCaptured(request.OrderId, paymentId, request.Amount)); }
        catch (Exception ex) { _logger.LogWarning(ex, "publish payment:captured failed for {orderId}", request.OrderId); }

        return BenzeneResult.Created(new PaymentTaken(paymentId, "captured"));
    }
}
