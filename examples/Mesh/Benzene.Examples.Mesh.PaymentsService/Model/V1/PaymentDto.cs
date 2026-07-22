namespace Benzene.Examples.Mesh.PaymentsService.Model.V1;

/// <summary>
/// Version 1 of the <c>payments:get</c> response payload. The original shape — no currency. A v1 client
/// (<c>GET /v1/payments/{id}</c>) gets this, downcast from the handler's canonical V2 by the payload caster
/// (docs/specification/versioning.md). Same type name in a per-version namespace is the caster convention
/// (<c>Benzene.Core.Versioning</c>'s <c>SchemaTypeMatcher</c>).
/// </summary>
public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Status { get; set; } = string.Empty;
}
