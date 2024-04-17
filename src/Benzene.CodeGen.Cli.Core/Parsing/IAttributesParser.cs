namespace Benzene.CodeGen.Cli.Core.Parsing;

public interface IAttributesParser
{
    IDictionary<string, string?> Parse(string[] args);
}