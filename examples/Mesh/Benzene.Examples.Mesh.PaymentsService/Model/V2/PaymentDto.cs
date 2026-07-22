namespace Benzene.Examples.Mesh.PaymentsService.Model.V2;

/// <summary>
/// Version 2 of the <c>payments:get</c> response payload — the current/canonical shape the single handler
/// produces. Adds <see cref="Currency"/> over <see cref="V1.PaymentDto"/>. A v2 client
/// (<c>GET /v2/payments/{id}</c> or the unversioned <c>/payments/{id}</c>, which resolves to latest) sees
/// this in full; a v1 client gets it downcast to <see cref="V1.PaymentDto"/> (currency dropped) — one
/// handler, both wire versions.
/// </summary>
public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}
