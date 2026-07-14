using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Http;
using Benzene.Mesh.Ui;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshUiMiddlewareTest
{
    public class FakeHttpContext : IHttpContext
    {
    }

    private static (Mock<IHttpRequestAdapter<FakeHttpContext>> RequestAdapter, Mock<IBenzeneResponseAdapter<FakeHttpContext>> ResponseAdapter)
        CreateAdapters(FakeHttpContext context, string method, string path)
    {
        var requestAdapter = new Mock<IHttpRequestAdapter<FakeHttpContext>>();
        requestAdapter.Setup(x => x.Map(context)).Returns(new HttpRequest { Method = method, Path = path });

        var responseAdapter = new Mock<IBenzeneResponseAdapter<FakeHttpContext>>();

        return (requestAdapter, responseAdapter);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("get")]
    [InlineData("HEAD")]
    public async Task HandleAsync_MatchingPath_WritesHtmlResponse(string method)
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, method, "/mesh-ui");
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/mesh-ui", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        responseAdapter.Verify(x => x.SetStatusCode(context, "200"), Times.Once);
        responseAdapter.Verify(x => x.SetContentType(context, "text/html; charset=utf-8"), Times.Once);
        responseAdapter.Verify(x => x.SetBody(context, MeshUiPage.GetHtml("manifest.json")), Times.Once);
        responseAdapter.Verify(x => x.FinalizeAsync(context), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonMatchingPath_CallsNext()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", "/other");
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/mesh-ui", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        responseAdapter.Verify(x => x.SetBody(context, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MatchingPath_WrongMethod_CallsNext()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "POST", "/mesh-ui");
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/mesh-ui", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        responseAdapter.Verify(x => x.SetBody(context, It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("/mesh-ui/")]
    [InlineData("/MESH-UI")]
    [InlineData("/Mesh-Ui/")]
    public async Task HandleAsync_PathNormalization_TrailingSlashAndCaseAreIgnored(string requestPath)
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", requestPath);
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/mesh-ui", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        responseAdapter.Verify(x => x.SetStatusCode(context, "200"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConfiguredPathWithTrailingSlashAndDifferentCase_StillMatches()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", "/mesh-ui");
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/Mesh-Ui/", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        responseAdapter.Verify(x => x.SetStatusCode(context, "200"), Times.Once);
    }

    [Fact]
    public void Name_IsMeshUi()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", "/mesh-ui");
        var middleware = new MeshUiMiddleware<FakeHttpContext>(
            "/mesh-ui", "manifest.json", requestAdapter.Object, responseAdapter.Object);

        Assert.Equal("MeshUi", middleware.Name);
    }
}
