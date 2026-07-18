using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.AwsMesh.Payments.Model;
using Benzene.Http;
using Benzene.Results;
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

/// <summary>Captures a payment for an order.</summary>
[HttpEndpoint("POST", "/payments")]
[Message("payments:capture")]
public class CapturePaymentMessageHandler : IMessageHandler<CapturePayment, PaymentDto>
{
    public Task<IBenzeneResult<PaymentDto>> HandleAsync(CapturePayment request)
        => BenzeneResult.Ok(new PaymentDto($"pay-{request.OrderId}", request.OrderId, request.Amount, request.Currency, "captured")).AsTask();
}

/// <summary>The request to capture a payment.</summary>
public class CapturePayment
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}
