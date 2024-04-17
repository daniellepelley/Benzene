namespace Benzene.CodeGen.Cli.Core;

public static class Constants
{
    public const string ProfileDescription = "Profile used to connect to AWS";
    public const string Profile = "profile";
    public const string LambdaName = "lambda-name";
    public const string LambdaNameDescription = "The name of the lambda running the service";
    public const string Output = "output";
    public const string OutputDefault = "client";
    public const string OutputDescription = "The build output, either 'client', 'message-handlers' or 'readme'";
    public const string Directory = "directory";
    public const string DirectoryDescription = "The destination directory for the code. Leave empty for current directory.";
    public const string ConfluenceUser = "confluence-user";
    public const string ConfluenceUserDescription = "The username for the Confluence account";
    public const string ConfluenceApiToken = "confluence-api-token";
    public const string ConfluenceApiTokenDescription = "The API token for the Confluence account";
    public const string ConfluencePage = "confluence-page";
    public const string ConfluencePageDescription = "The page in Confluence to be updated";
    public const string ConfluenceAttachmentPrefix = "confluence-attachment-prefix";
    public const string ConfluenceAttachmentPrefixDescription = "The prefix for attachments in Confluence";
    public const string Type = "type";
    public const string TypeDefault = "benzene";
    public const string TypeDescription = "The document type, either 'benzene', 'openapi' or 'asyncapi'";
    public const string Format = "format";
    public const string FormatDefault = "json";
    public const string FormatDescription = "The document format, either 'yaml' or 'json'";
}
