namespace Benzene.Examples.K8sMesh.Service.Model.V2;

/// <summary>
/// Version 2 of the <c>payment:take</c> request — the current/canonical shape the single handler is written
/// against. Adds <see cref="Currency"/> over <see cref="V1.TakePaymentRequest"/>. A v1 payload is upcast to
/// this before the handler runs, the caster seeding <see cref="Currency"/> with a default — so the handler
/// only ever sees v2, whichever version the producer sent.
/// </summary>
public class TakePaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
