namespace Benzene.Examples.Mesh.OrdersService.Model;

public class OrderDto
{
    public OrderDto(string id, string item, int quantity)
    {
        Id = id;
        Item = item;
        Quantity = quantity;
    }

    public string Id { get; }

    public string Item { get; }

    public int Quantity { get; }
}
