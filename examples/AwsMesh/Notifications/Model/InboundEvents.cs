namespace Benzene.Examples.AwsMesh.Notifications.Model;

/// <summary>The <c>order:placed</c> event as notifications-api consumes it (over SNS).</summary>
public class OrderPlaced
{
    public string OrderId { get; set; } = "";
    public string Item { get; set; } = "";
}

/// <summary>The <c>payment:captured</c> event as notifications-api consumes it (over EventBridge).</summary>
public class PaymentCaptured
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}

/// <summary>The <c>shipping:dispatched</c> event as notifications-api consumes it (over EventBridge).</summary>
public class ShipmentDispatched
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
    public string TrackingNumber { get; set; } = "";
}
