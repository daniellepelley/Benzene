using System;

namespace Benzene.Test.Autogen.CodeGen.Model;

public class CreateUserMessage
{
    public string Name { get; set; }
    public string[] Tenants { get; set; }
    public DateTime? Date { get; set; }
    public Guid? Ref { get; set; }
}
