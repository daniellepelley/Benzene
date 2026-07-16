using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.Shared;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.Mesh.OrdersService.Handlers;

public class CheckoutRequest
{
    public string? OrderId { get; set; }
}

public class CheckoutReply
{
    public string Confirmation { get; set; } = string.Empty;
}

/// <summary>
/// The cross-service hop of the mesh demo: checkout calls payments-api over the wire envelope,
/// and <see cref="EnvelopeClient"/> forwards the current mesh span as a traceparent header - which
/// is what lets the collector derive the orders-api → payments:get consumer edge and join the two
/// services' events into one flow on the Fleet view. That propagation is the only mesh-specific
/// behavior in this handler.
/// </summary>
[HttpEndpoint("POST", "/checkout")]
[Message("orders:checkout")]
public class CheckoutOrderMessageHandler : IMessageHandler<CheckoutRequest, CheckoutReply>
{
    private static readonly string PaymentsEnvelopeUrl =
        Environment.GetEnvironmentVariable("PAYMENTS_ENVELOPE_URL") ?? "http://localhost:5311/benzene/invoke";

    public async Task<IBenzeneResult<CheckoutReply>> HandleAsync(CheckoutRequest request)
    {
        var orderId = string.IsNullOrEmpty(request.OrderId) ? "ord-1" : request.OrderId!;
        var (statusCode, body) = await EnvelopeClient.SendAsync(
            PaymentsEnvelopeUrl, "payments:get", $"{{\"id\":\"pay-{orderId}\"}}");

        if (statusCode != BenzeneResultStatus.Ok)
        {
            return BenzeneResult.ServiceUnavailable<CheckoutReply>($"payments-api returned {statusCode}");
        }
        return BenzeneResult.Ok(new CheckoutReply { Confirmation = $"order {orderId} paid: {body}" });
    }
}
