using System;

namespace Benzene.Test.Autogen.CodeGen.Model;

public class TenantDto : IHasId
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Crn { get; set; }
    public InternalDto Internal { get; set; }
}
