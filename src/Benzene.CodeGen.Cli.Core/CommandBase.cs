using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core;

public abstract class CommandBase<TPayload> : ICommand<TPayload> where TPayload : new()
{
    public string Name { get; }
    public string Description { get; }

    protected CommandBase(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string GetHelp()
    {
        return HelpGenerator.Generate<TPayload>(Name, Description);
    }

    public async Task ExecuteAsync(CommandArguments commandArguments)
    {
        var payload = PayloadMapper.Map<TPayload>(commandArguments);
        await ExecuteAsync(payload);
    }

    public abstract Task ExecuteAsync(TPayload commandPayload);
}
