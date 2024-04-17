using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core;

public class CommandRouter
{
    private readonly ICommand[] _commands;

    public CommandRouter(params ICommand[] commands)
    {
        _commands = commands.Concat(new []
        {
            new HelpCommand(commands)
        }).ToArray();
    }

    public Task RouteAsync(CommandArguments args)
    {
        var command = _commands.FirstOrDefault(x => x.Name == args.Name);
        if (command == null)
        {
            Console.Error.WriteLine($"Command {args.Name} not found");
            return Task.CompletedTask;
        }

        return command.ExecuteAsync(args);
    }
}
