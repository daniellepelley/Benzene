namespace Benzene.Examples.AwsMesh.Shipping.Model;

/// <summary>A shipment in the shipping-api domain (a slightly different model than orders/payments).</summary>
public class ShipmentDto
{
    public ShipmentDto(string id, string orderId, string carrier, string trackingNumber, string status)
    {
        Id = id;
        OrderId = orderId;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        Status = status;
    }

    public string Id { get; }
    public string OrderId { get; }
    public string Carrier { get; }
    public string TrackingNumber { get; }
    public string Status { get; }
}
