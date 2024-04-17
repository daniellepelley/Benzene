using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    string GetHelp();
    Task ExecuteAsync(CommandArguments commandArguments);
}

public interface ICommand<TPayload> : ICommand where TPayload : new()
{
    Task ExecuteAsync(TPayload commandPayload);
}
