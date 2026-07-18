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
