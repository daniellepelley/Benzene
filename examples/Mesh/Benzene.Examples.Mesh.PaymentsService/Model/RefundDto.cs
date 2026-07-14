namespace Benzene.Examples.Mesh.PaymentsService.Model;

public class RefundDto
{
    public RefundDto(string id, int amountCents)
    {
        Id = id;
        AmountCents = amountCents;
    }

    public string Id { get; }

    public int AmountCents { get; }
}
