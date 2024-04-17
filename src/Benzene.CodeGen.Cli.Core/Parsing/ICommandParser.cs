namespace Benzene.CodeGen.Cli.Core.Parsing;

public interface ICommandParser
{
    CommandArguments Parse(string[] args);
}