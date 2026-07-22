namespace Benzene.Examples.AwsMesh.Inventory.Model;

/// <summary>
/// The <c>order:placed</c> event as inventory-api consumes it (arrives over SNS). Only the fields this
/// service cares about — extra fields the publisher sends are ignored on deserialization.
/// </summary>
public class OrderPlaced
{
    public string OrderId { get; set; } = "";
    public string Item { get; set; } = "";
    public int Quantity { get; set; }
}

/// <summary>
/// The <c>shipping:dispatched</c> event as inventory-api consumes it (arrives over EventBridge) — the
/// cue to turn a stock reservation into a decrement.
/// </summary>
public class ShipmentDispatched
{
    public string OrderId { get; set; } = "";
    public string ShipmentId { get; set; } = "";
}
