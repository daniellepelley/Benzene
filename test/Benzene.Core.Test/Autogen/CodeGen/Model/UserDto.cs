namespace Benzene.Test.Autogen.CodeGen.Model;

public class UserDto : IHasId<string>
{
    public string Id { get; set; }
    public string[] TenantIds { get; set; }
    public InternalDto Internal { get; set; }
}
