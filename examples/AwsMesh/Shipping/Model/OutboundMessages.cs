namespace Benzene.Examples.AwsMesh.Shipping.Model;

/// <summary>
/// The <c>shipping:dispatched</c> integration event shipping-api publishes to EventBridge once a
/// shipment is on its way — the terminal event of the chain. EventBridge routes it by rule to
/// inventory-api (decrement stock), notifications-api (tell the customer) and analytics-api (metrics).
/// Declared in the spec's <c>events</c> → structural edges shipping → inventory / notifications / analytics.
/// </summary>
public class OutboundShipmentDispatched
{
    public string OrderId { get; set; } = "";
    public string ShipmentId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
    public string TrackingNumber { get; set; } = "";
}
