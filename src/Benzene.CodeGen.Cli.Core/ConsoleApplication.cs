using Benzene.CodeGen.Cli.Core.Commands.Build;
using Benzene.CodeGen.Cli.Core.Commands.Confluence;
using Benzene.CodeGen.Cli.Core.Commands.HealthCheck;
using Benzene.CodeGen.Cli.Core.Commands.Spec;
using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core;

public class ConsoleApplication
{
    private readonly CommandParser _commandParser = new();
    private readonly CommandSplitter _commandSplitter = new();
    private readonly CommandRouter _router;

    public ConsoleApplication()
    {
        _router = new CommandRouter(
            new BuildCommand(),
            new HealthCheckCommand(),
            new SpecCommand(),
            new ConfluenceCommand()
            );
    }

    public Task ExecuteAsync(string args)
    {
        return ExecuteAsync(_commandSplitter.Split(args));
    }
    
    public Task ExecuteAsync(string[] args)
    {
        var commandArguments = _commandParser.Parse(args);
        return _router.RouteAsync(commandArguments);
    }
}
