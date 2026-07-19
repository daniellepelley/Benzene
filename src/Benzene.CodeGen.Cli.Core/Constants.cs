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
    public const string File = "file";
    public const string FileDescription = "Path to a benzene spec JSON file to read instead of fetching the spec from a lambda";
    public const string ConfluenceBaseUrl = "confluence-base-url";
    public const string ConfluenceBaseUrlDefault = "https://ngbenzene.atlassian.net";
    public const string ConfluenceBaseUrlDescription = "The base URL of the Confluence (Atlassian) instance, e.g. 'https://your-org.atlassian.net'";
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
    public const string Url = "url";
    public const string UrlDescription = "The base URL of the Benzene Cloud Service to probe, e.g. https://orders.example.com";
    public const string InvokePath = "invoke-path";
    public const string InvokePathDescription = "Overrides the wire-envelope endpoint path probed for R4/R6. Defaults to /benzene/invoke";
    public const string SpecPath = "spec-path";
    public const string SpecPathDescription = "Overrides the derived spec endpoint path probed for R5. Defaults to /benzene/spec";
    public const string HealthPath = "health-path";
    public const string HealthPathDescription = "Overrides the health endpoint path probed for R3. Defaults to /benzene/health";
    public const string NoTraceParentProbe = "no-traceparent-probe";
    public const string NoTraceParentProbeDescription = "Skips the R8 bonus traceparent header sent with the R4/R6 probe requests";
}
