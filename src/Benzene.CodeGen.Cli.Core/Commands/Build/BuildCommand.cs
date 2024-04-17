namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public class BuildCommand : CommandBase<BuildPayload>
{
    public BuildCommand()
        : base("build", "Builds code, config or documentation for a Benzene service")
    { }

    public override async Task ExecuteAsync(BuildPayload payload)
    {
        await new ClientCodeBuilder().Build(payload);
    }
}
