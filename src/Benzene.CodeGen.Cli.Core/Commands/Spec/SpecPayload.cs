using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core.Commands.Spec;

public class SpecPayload
{
    [Arg(Name = Constants.Profile, Description = Constants.ProfileDescription)]
    public string Profile { get; set; }
    [Arg(Name = Constants.LambdaName, Description = Constants.LambdaNameDescription)]
    public string LambdaName { get; set; }
    [Arg(Name = Constants.Type, DefaultValue = Constants.TypeDefault, Description = Constants.TypeDescription)]
    public string Type { get; set; }
    [Arg(Name = Constants.Format, DefaultValue = Constants.FormatDefault, Description = Constants.FormatDescription)]
    public string Format { get; set; }
}

