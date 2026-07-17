using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core.Commands.CloudServiceProfile;

public class CloudServiceProfileCheckPayload
{
    [Arg(Name = Constants.Url, Description = Constants.UrlDescription)]
    public string Url { get; set; }

    [Arg(Name = Constants.InvokePath, Description = Constants.InvokePathDescription)]
    public string InvokePath { get; set; }

    [Arg(Name = Constants.SpecPath, Description = Constants.SpecPathDescription)]
    public string SpecPath { get; set; }

    [Arg(Name = Constants.HealthPath, Description = Constants.HealthPathDescription)]
    public string HealthPath { get; set; }

    [Arg(Name = Constants.NoTraceParentProbe, DefaultValue = "false", Description = Constants.NoTraceParentProbeDescription)]
    public string NoTraceParentProbe { get; set; }
}
