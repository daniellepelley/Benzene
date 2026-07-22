namespace Benzene.Examples.AwsMesh.Analytics.Model;

/// <summary>The <c>payment:captured</c> event as analytics-api consumes it (over EventBridge) — revenue.</summary>
public class PaymentCaptured
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}

/// <summary>The <c>shipping:dispatched</c> event as analytics-api consumes it (over EventBridge) — fulfilment.</summary>
public class ShipmentDispatched
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
}
