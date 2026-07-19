using System.Net.Http.Headers;
using System.Text;

namespace Benzene.CodeGen.Cli.Core.Confluence;

public class ConfluenceClient
{
    /// <summary>
    /// The default Confluence (Atlassian) base URL. It is a single default, not a hard-coded value —
    /// pass a different base URL to the constructor (or via the <c>confluence-base-url</c> CLI arg) to
    /// publish to another organization's Confluence instance.
    /// </summary>
    public const string DefaultBaseUrl = "https://ngbenzene.atlassian.net";

    private readonly string _baseUrl;
    private readonly string _confluenceUser;
    private readonly string _confluenceApiToken;
    private readonly string _confluencePage;

    public ConfluenceClient(string confluenceUser, string confluenceApiToken, string confluencePage, string baseUrl = DefaultBaseUrl)
    {
        _confluencePage = confluencePage;
        _confluenceApiToken = confluenceApiToken;
        _confluenceUser = confluenceUser;
        _baseUrl = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
    }
    public async Task UploadFileAsync(string content, string id)
    {
        var credentials = $"{_confluenceUser}:{_confluenceApiToken}";
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
        var url = $"{_baseUrl}/wiki/rest/api/content/{_confluencePage}/child/attachment";
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            client.DefaultRequestHeaders.Add("X-Atlassian-Token", "no-check");
            using (var multipartFormDataContent = new MultipartFormDataContent())
            {
                var fileContent = new StringContent(content);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                multipartFormDataContent.Add(fileContent, "file", id);
                var response = await client.PutAsync(url, multipartFormDataContent);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
