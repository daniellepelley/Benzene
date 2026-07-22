namespace Benzene.Examples.K8sMesh.Service.Model.V1;

/// <summary>
/// Version 1 of the <c>payment:take</c> request — no currency. orders-api is pinned to v1 and sends this
/// (declaring <c>benzene-version: 1</c> via <c>SendMessageAsync(..., version: "1")</c>); payments-api's
/// single v2 handler never sees it directly — the payload caster upcasts it to
/// <see cref="V2.TakePaymentRequest"/> first (docs/specification/versioning.md). Same type name in a
/// per-version namespace is the caster convention.
/// </summary>
public class TakePaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
