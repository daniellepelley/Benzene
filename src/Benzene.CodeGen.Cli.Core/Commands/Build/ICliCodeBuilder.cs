namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public interface ICliCodeBuilder
{
    Task Build(BuildPayload payload);
}