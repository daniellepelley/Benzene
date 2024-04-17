namespace Benzene.CodeGen.Cli.Core.Parsing;

public class CommandArguments
{
    public string Name { get; set; }
    public string? Value { get; set; }
    public IDictionary<string, string?> Attributes { get; set; }
}
