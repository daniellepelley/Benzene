namespace Benzene.Examples.Mesh.ShippingService.Model;

public class ShipmentDto
{
    public ShipmentDto(string id, string carrier, string status)
    {
        Id = id;
        Carrier = carrier;
        Status = status;
    }

    public string Id { get; }

    public string Carrier { get; }

    public string Status { get; }
}
