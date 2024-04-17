namespace Benzene.Test.Autogen.CodeGen.Model;

public class CreateTenantMessage
{
    public string Name { get; set; }
    public string Crn { get; set; }
}

public interface IHasId<TId>
{
    TId Id { get; }
}
