using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core;

public class HelpCommand : ICommand
{
    private readonly ICommand[] _commands;

    public HelpCommand(ICommand[] commands)
    {
        _commands = commands;
    }
    
    public string Name { get; } = "help";
    public string Description { get; } = "Displays the available commands and their descriptions.";
    public string GetHelp()
    {
        return string.Empty;
    }

    public Task ExecuteAsync(CommandArguments commandArguments)
    {
        if (string.IsNullOrEmpty(commandArguments.Value))
        {
            Console.WriteLine("Benzene");
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            foreach (var command in _commands)
            {
                Console.WriteLine($"  {command.Name,-40} {command.Description}");
            }
        }
        else
        {
            var command = _commands.FirstOrDefault(x => x.Name == commandArguments.Value);
            Console.WriteLine(command.GetHelp());
        }

        return Task.CompletedTask;
    }
}
