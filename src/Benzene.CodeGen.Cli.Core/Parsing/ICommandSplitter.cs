namespace Benzene.CodeGen.Cli.Core.Parsing;

public interface ICommandSplitter
{
    string[] Split(string args);
}