namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public interface ICommandPayload
{
    string LambdaName { get; }
    string Output { get; }
}
