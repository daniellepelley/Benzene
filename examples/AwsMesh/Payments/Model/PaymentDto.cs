namespace Benzene.Examples.AwsMesh.Payments.Model;

/// <summary>A payment in the payments-api domain (a slightly different model than orders/shipping).</summary>
public class PaymentDto
{
    public PaymentDto(string id, string orderId, decimal amount, string currency, string status)
    {
        Id = id;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        Status = status;
    }

    public string Id { get; }
    public string OrderId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string Status { get; }
}
