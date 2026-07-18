namespace Benzene.Examples.AwsMesh.Orders.Model;

// The messages orders-api sends downstream after an order is placed. Declaring them (see Startup)
// puts them in the service's spec `events`, which the mesh turns into structural topology edges
// (orders → payments, orders → shipping).

/// <summary>The capture-payment request orders-api sends to payments-api (topic <c>payments:capture</c>).</summary>
public class OutboundPaymentCapture
{
    public string OrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}

/// <summary>The book-shipment request orders-api sends to shipping-api (topic <c>shipping:book</c>).</summary>
public class OutboundShipmentBook
{
    public string OrderId { get; set; } = "";
    public string Carrier { get; set; } = "DPD";
}
