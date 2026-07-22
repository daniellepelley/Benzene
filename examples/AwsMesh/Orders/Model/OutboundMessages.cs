namespace Benzene.Examples.AwsMesh.Orders.Model;

/// <summary>
/// The capture-payment request orders-api sends downstream to payments-api (topic
/// <c>payments:capture</c>) after an order is placed. Declaring it (see Startup) puts it in the
/// service's spec <c>events</c>, which the mesh turns into the structural edge orders → payments.
/// </summary>
public class OutboundPaymentCapture
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}

/// <summary>
/// The <c>order:placed</c> event orders-api publishes to SNS once an order is placed. SNS fans it out
/// to <b>every</b> subscriber — here both inventory-api (reserve stock) and notifications-api (notify the
/// customer) — which is what SNS is for, versus the point-to-point SQS command to payments-api. Declared
/// in the spec's <c>events</c> → the mesh's structural edges orders → inventory and orders → notifications.
/// </summary>
public class OutboundOrderPlaced
{
    public string OrderId { get; set; } = "";
    public string Item { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}
