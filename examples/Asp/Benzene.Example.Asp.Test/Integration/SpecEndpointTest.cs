using System.Net;
using Benzene.Examples.App.Handlers;
using Xunit;

namespace Benzene.Example.Asp.Test.Integration;

/// <summary>
/// Covers the Asp example's spec surface - the canonical demonstration of Benzene's spec/schema
/// generation (<c>UseSpec()</c>) and the Spec Explorer UI (<c>UseSpecUi()</c>), wired in
/// <c>Startup.Configure</c>. Nothing else in the example suite exercised these before, even though
/// <c>examples/CLAUDE.md</c> calls this folder out as "where the Spec UI (/spec-ui) and the spec
/// endpoint are wired". A regression that stopped the spec listing the app's handlers - or stopped
/// the UI serving - would now fail the build.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Sequential")]
public class SpecEndpointTest : InMemoryOrdersTestBase
{
    [Fact]
    public async Task Spec_Benzene_ListsTheAppsMessageTopics()
    {
        // The URL the Spec Explorer UI itself fetches on load (SpecUiExtensions.DefaultSpecUrl).
        var response = await _client.GetAsync("/spec?type=benzene&format=json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var spec = await response.Content.ReadAsStringAsync();

        // The generated spec must describe the handlers this example actually registers - proving the
        // spec is derived from the live handler registry, not a static document that can silently rot.
        Assert.Contains(MessageTopicNames.OrderCreate, spec);
        Assert.Contains(MessageTopicNames.OrderGet, spec);
        Assert.Contains(MessageTopicNames.OrderGetAll, spec);
    }

    [Fact]
    public async Task SpecUi_ServesTheSpecExplorerPage()
    {
        var response = await _client.GetAsync("/spec-ui");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // The bundled Spec Explorer page (Benzene.Spec.Ui/spec-ui.html) - a marker only that page carries.
        Assert.Contains("Benzene Spec Explorer", html);
    }
}
