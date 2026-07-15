using Benzene.Spec.Ui;
using Xunit;

namespace Benzene.Test.SpecUi;

public class SpecUiPageTest
{
    [Fact]
    public void GetHtml_ReturnsTheEmbeddedPage()
    {
        var html = SpecUiPage.GetHtml();

        Assert.Contains("<html lang=\"en\">", html);
    }

    [Fact]
    public void GetHtml_WithSpecUrl_InjectsDataSpecUrlAttribute()
    {
        var html = SpecUiPage.GetHtml("/spec?type=benzene");

        Assert.Contains("<html lang=\"en\" data-spec-url=\"/spec?type=benzene\">", html);
    }

    [Fact]
    public void GetHtml_WithSpecUrlContainingSpecialCharacters_HtmlEncodesTheAttributeValue()
    {
        var html = SpecUiPage.GetHtml("/spec?type=benzene&env=\"prod\"");

        Assert.Contains("data-spec-url=\"/spec?type=benzene&amp;env=&quot;prod&quot;\"", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetHtml_NullOrWhitespaceSpecUrl_BehavesLikeParameterlessOverload(string specUrl)
    {
        var html = SpecUiPage.GetHtml(specUrl);

        // The embedded page's own JS references the literal string "data-spec-url" (it reads the
        // attribute at runtime), so it's always present in the markup — only the equality check
        // against the parameterless overload proves no attribute was injected.
        Assert.Equal(SpecUiPage.GetHtml(), html);
    }
}
