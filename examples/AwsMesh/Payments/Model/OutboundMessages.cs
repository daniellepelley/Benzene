namespace Benzene.Examples.AwsMesh.Payments.Model;

/// <summary>
/// The book-shipment request payments-api sends downstream to shipping-api (topic
/// <c>shipping:book</c>) after a payment is captured — the second hop of the
/// order → payment → shipment chain. Declared in the spec's <c>events</c> → structural edge
/// payments → shipping.
/// </summary>
public class OutboundShipmentBook
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
}

/// <summary>
/// The <c>payment:captured</c> integration event payments-api publishes to EventBridge once a payment
/// settles. EventBridge routes it by rule to whoever cares — notifications-api and analytics-api here —
/// which is what EventBridge is for (content-routed integration events) versus the point-to-point SQS
/// command to shipping-api. Declared in the spec's <c>events</c> → structural edges payments →
/// notifications and payments → analytics.
/// </summary>
public class OutboundPaymentCaptured
{
    public string OrderId { get; set; } = "";
    public string PaymentId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}
