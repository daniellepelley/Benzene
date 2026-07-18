namespace Benzene.Examples.AwsMesh.Orders.Model;

/// <summary>An order in the orders-api domain.</summary>
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
