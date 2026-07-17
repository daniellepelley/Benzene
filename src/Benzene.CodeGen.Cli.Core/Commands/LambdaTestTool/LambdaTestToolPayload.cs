using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core.Commands.LambdaTestTool;

public class LambdaTestToolPayload
{
    [Arg(Name = Constants.Profile, Description = Constants.ProfileDescription)]
    public string Profile { get; set; }
    [Arg(Name = Constants.LambdaName, Description = Constants.LambdaNameDescription)]
    public string LambdaName { get; set; }
    [Arg(Name = Constants.File, Description = Constants.FileDescription)]
    public string File { get; set; }
    [Arg(Name = Constants.Directory, Description = Constants.DirectoryDescription)]
    public string Directory { get; set; }
}
