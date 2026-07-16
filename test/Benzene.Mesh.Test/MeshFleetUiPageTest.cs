using Benzene.Mesh.Ui;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshFleetUiPageTest
{
    [Fact]
    public void GetHtml_ReturnsEmbeddedPage()
    {
        var html = MeshFleetUiPage.GetHtml();

        Assert.Contains("<title>Benzene Mesh — Fleet</title>", html);
        Assert.Contains("mesh:query:fleet", html);
        Assert.Contains("<html lang=\"en\">", html);
    }

    [Fact]
    public void GetHtml_WithUrl_InjectsEnvelopeUrlAttribute()
    {
        var html = MeshFleetUiPage.GetHtml("/collector/invoke");

        Assert.Contains("<html lang=\"en\" data-envelope-url=\"/collector/invoke\">", html);
    }

    [Fact]
    public void GetHtml_WithUrl_HtmlEncodesTheUrl()
    {
        var html = MeshFleetUiPage.GetHtml("https://example.com/invoke?tenant=a&b=c");

        Assert.Contains("data-envelope-url=\"https://example.com/invoke?tenant=a&amp;b=c\"", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetHtml_WithNullOrWhitespaceUrl_BehavesLikeGetHtml(string? envelopeUrl)
    {
        var html = MeshFleetUiPage.GetHtml(envelopeUrl!);

        Assert.Equal(MeshFleetUiPage.GetHtml(), html);
        Assert.Contains("<html lang=\"en\">", html);
    }
}
