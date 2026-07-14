using Benzene.Mesh.Ui;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshUiPageTest
{
    [Fact]
    public void GetHtml_ReturnsEmbeddedPage()
    {
        var html = MeshUiPage.GetHtml();

        Assert.Contains("<title>Benzene Mesh Explorer</title>", html);
        Assert.Contains("id=\"benzene-mesh-data\"", html);
        Assert.Contains("<html lang=\"en\">", html);
    }

    [Fact]
    public void GetHtml_WithUrl_InjectsManifestUrlAttribute()
    {
        var html = MeshUiPage.GetHtml("https://example.com/manifest.json");

        Assert.Contains(
            "<html lang=\"en\" data-manifest-url=\"https://example.com/manifest.json\">",
            html);
    }

    [Fact]
    public void GetHtml_WithUrl_HtmlEncodesTheUrl()
    {
        var html = MeshUiPage.GetHtml("https://example.com/manifest.json?tenant=a&b=c");

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
        var html = MeshUiPage.GetHtml(manifestUrl!);

        Assert.Equal(MeshUiPage.GetHtml(), html);
        Assert.Contains("<html lang=\"en\">", html);
    }
}
