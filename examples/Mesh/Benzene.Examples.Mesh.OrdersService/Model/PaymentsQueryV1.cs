namespace Benzene.Examples.Mesh.OrdersService.Model;

/// <summary>
/// The (version 1) shape orders-api declares it sends when it asks payments-api for a payment
/// (topic <c>payments:get</c>). orders-api is pinned to v1 while payments-api's handler has already moved
/// to v2 — declaring this puts a <c>payments:get@1</c> producer in the fleet, so the mesh's version
/// compatibility view surfaces the skew (produced v1, consumed v2), which payments-api's upcaster bridges.
/// </summary>
public class PaymentsQueryV1
{
    public string Id { get; set; } = string.Empty;
}
