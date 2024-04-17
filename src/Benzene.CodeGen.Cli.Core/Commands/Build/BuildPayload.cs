using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public class BuildPayload : ICommandPayload
{
    [Arg(Name = Constants.Profile, Description = Constants.ProfileDescription)]
    public string Profile { get; set; }
    [Arg(Name = Constants.LambdaName, Description = Constants.LambdaNameDescription)]
    public string LambdaName { get; set; }
    [Arg(Name = Constants.Output, DefaultValue = Constants.OutputDefault, Description = Constants.OutputDescription)]
    public string Output { get; set; }
    [Arg(Name = Constants.Directory, Description = Constants.DirectoryDescription)]
    public string Directory { get; set; }
}

