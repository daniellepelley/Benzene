namespace Benzene.CodeGen.Cli.Core.Parsing;

public class ArgAttribute : Attribute
{
    public string Name { get; set; }
    public string DefaultValue { get; set; }
    public string Description { get; set; }
}