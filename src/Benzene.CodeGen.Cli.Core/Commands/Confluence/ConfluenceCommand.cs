namespace Benzene.CodeGen.Cli.Core.Commands.Confluence;

public class ConfluenceCommand : CommandBase<ConfluencePayload>
{
    public ConfluenceCommand()
        : base("confluence", "Creates documentation in confluence")
    { }
    public override async Task ExecuteAsync(ConfluencePayload payload)
    {
        await new ConfluenceBuilder().Build(payload);
    }
}

