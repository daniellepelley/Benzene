namespace Benzene.Examples.Mesh.PaymentsService.Model;

public class PaymentDto
{
    public PaymentDto(string id, int amountCents, string status)
    {
        Id = id;
        AmountCents = amountCents;
        Status = status;
    }

    public string Id { get; }

    public int AmountCents { get; }

    public string Status { get; }
}
