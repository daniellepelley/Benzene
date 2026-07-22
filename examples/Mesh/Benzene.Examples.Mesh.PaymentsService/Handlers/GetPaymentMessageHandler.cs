using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.Http;
using Benzene.Results;
using V2 = Benzene.Examples.Mesh.PaymentsService.Model.V2;

namespace Benzene.Examples.Mesh.PaymentsService.Handlers;

/// <summary>
/// The single, canonical (version 2) <c>payments:get</c> handler. It only ever sees — and only ever
/// produces — the V2 payload; older/newer wire versions are bridged by the payload casters wired in
/// <c>Startup</c> (docs/specification/versioning.md). With path-based versioning on (<c>AddHttpVersioning</c>),
/// it answers <c>/payments/{id}</c> (latest), <c>/v2/payments/{id}</c> (V2), and <c>/v1/payments/{id}</c>
/// (the V2 response downcast to V1 — currency dropped) all from this one handler.
/// </summary>
[HttpEndpoint("GET", "/payments/{id}")]
[Message("payments:get", "2")]
public class GetPaymentMessageHandler : IMessageHandler<GetPaymentMessage, V2.PaymentDto>
{
    public Task<IBenzeneResult<V2.PaymentDto>> HandleAsync(GetPaymentMessage request)
    {
        return BenzeneResult.Ok(new V2.PaymentDto
        {
            Id = request.Id,
            AmountCents = 4999,
            Status = "Captured",
            Currency = "GBP",
        }).AsTask();
    }
}
