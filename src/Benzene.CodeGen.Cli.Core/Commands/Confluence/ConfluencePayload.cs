using Benzene.CodeGen.Cli.Core.Commands.Build;
using Benzene.CodeGen.Cli.Core.Parsing;

namespace Benzene.CodeGen.Cli.Core.Commands.Confluence;

public class ConfluencePayload : ICommandPayload
{
    [Arg(Name = Constants.Profile, Description = Constants.ProfileDescription)]
    public string Profile { get; set; }
    [Arg(Name = Constants.LambdaName, Description = Constants.LambdaNameDescription)]
    public string LambdaName { get; set; }
    [Arg(Name = Constants.Output, Description = Constants.OutputDescription)]
    public string Output { get; set; }
    [Arg(Name = Constants.ConfluenceUser, Description = Constants.ConfluenceUserDescription)]
    public string ConfluenceUser { get; set; }
    [Arg(Name = Constants.ConfluenceApiToken, Description = Constants.ConfluenceApiTokenDescription)]
    public string ConfluenceApiToken { get; set; }
    [Arg(Name = Constants.ConfluencePage, Description = Constants.ConfluencePageDescription)]
    public string ConfluencePage { get; set; }
    [Arg(Name = Constants.ConfluenceAttachmentPrefix, Description = Constants.ConfluenceAttachmentPrefixDescription)]
    public string ConfluenceAttachmentPrefix { get; set; }
}
