using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Examples.Mesh.PaymentsService.Model;
using Benzene.Results;

namespace Benzene.Examples.Mesh.PaymentsService.Handlers;

/// <summary>
/// Deliberately has no <c>[HttpEndpoint]</c>/<c>[Message]</c> attributes, since reflection-based
/// handler discovery scans every loaded assembly unconditionally by attribute presence - it can't
/// be toggled off at runtime. Instead this is wired up manually in <c>Startup.cs</c>, only when
/// <c>DEMO_ADD_ENDPOINT=true</c>: adding this operation changes the generated OpenAPI spec, so
/// restarting Payments with that flag set and re-running the aggregator demonstrates real
/// contract drift (the same manual-registration technique <c>SpecMessageHandler</c> itself uses).
/// </summary>
public class GetPaymentRefundsMessageHandler : IMessageHandler<GetPaymentMessage, RefundDto[]>
{
    public Task<IBenzeneResult<RefundDto[]>> HandleAsync(GetPaymentMessage request)
    {
        return BenzeneResult.Ok(Array.Empty<RefundDto>()).AsTask();
    }
}
