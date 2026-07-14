using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.Mesh.PaymentsService.Handlers;

[HttpEndpoint("GET", "/payments/{id}")]
[Message("payments:get")]
public class GetPaymentMessageHandler : IMessageHandler<GetPaymentMessage, PaymentDto>
{
    public Task<IBenzeneResult<PaymentDto>> HandleAsync(GetPaymentMessage request)
    {
        return BenzeneResult.Ok(new PaymentDto(request.Id, 4999, "Captured")).AsTask();
    }
}
