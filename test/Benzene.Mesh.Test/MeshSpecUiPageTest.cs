using Benzene.Mesh.Ui;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshSpecUiPageTest
{
    [Fact]
    public void GetHtml_ReturnsEmbeddedPage()
    {
        var html = MeshSpecUiPage.GetHtml();

        Assert.Contains("<title>Benzene Mesh — Service Spec</title>", html);
        Assert.Contains("<html lang=\"en\">", html);
        // Loads from the same-origin snapshot the aggregator captured, not a service-hosted UI.
        Assert.Contains("services/", html);
        Assert.Contains("specJson", html);
    }

    [Fact]
    public void GetHtml_WithUrl_InjectsManifestUrlAttribute()
    {
        var html = MeshSpecUiPage.GetHtml("https://example.com/manifest.json");

        Assert.Contains(
            "<html lang=\"en\" data-manifest-url=\"https://example.com/manifest.json\">",
            html);
    }

    [Fact]
    public void GetHtml_WithUrl_HtmlEncodesTheUrl()
    {
        var html = MeshSpecUiPage.GetHtml("https://example.com/manifest.json?tenant=a&b=c");

        Assert.Contains(
            "data-manifest-url=\"https://example.com/manifest.json?tenant=a&amp;b=c\"",
            html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetHtml_WithNullOrWhitespaceUrl_BehavesLikeGetHtml(string? manifestUrl)
    {
        var html = MeshSpecUiPage.GetHtml(manifestUrl!);

        Assert.Equal(MeshSpecUiPage.GetHtml(), html);
        Assert.Contains("<html lang=\"en\">", html);
    }
}
