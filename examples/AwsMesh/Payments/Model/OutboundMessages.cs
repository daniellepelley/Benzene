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
