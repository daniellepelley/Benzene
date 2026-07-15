using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Http;
using Benzene.Spec.Ui;
using Moq;
using Xunit;

namespace Benzene.Test.SpecUi;

public class SpecUiMiddlewareTest
{
    private class FakeHttpContext : IHttpContext
    {
    }

    private static (SpecUiMiddleware<FakeHttpContext> Middleware, Mock<IBenzeneResponseAdapter<FakeHttpContext>> ResponseAdapterMock) CreateMiddleware(
        string configuredPath, string method, string requestPath)
    {
        var requestAdapterMock = new Mock<IHttpRequestAdapter<FakeHttpContext>>();
        requestAdapterMock.Setup(x => x.Map(It.IsAny<FakeHttpContext>()))
            .Returns(new HttpRequest { Method = method, Path = requestPath });

        var responseAdapterMock = new Mock<IBenzeneResponseAdapter<FakeHttpContext>>();

        var middleware = new SpecUiMiddleware<FakeHttpContext>(
            configuredPath, "/spec?type=benzene", requestAdapterMock.Object, responseAdapterMock.Object);

        return (middleware, responseAdapterMock);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("get")]
    public async Task HandleAsync_MatchingMethodAndPath_WritesHtmlResponseAndShortCircuits(string method)
    {
        var (middleware, responseAdapterMock) = CreateMiddleware("/spec-ui", method, "/spec-ui");
        var nextCalled = false;

        await middleware.HandleAsync(new FakeHttpContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        responseAdapterMock.Verify(x => x.SetStatusCode(It.IsAny<FakeHttpContext>(), "200"), Times.Once);
        responseAdapterMock.Verify(x => x.SetContentType(It.IsAny<FakeHttpContext>(), "text/html; charset=utf-8"), Times.Once);
        responseAdapterMock.Verify(x => x.SetBody(It.IsAny<FakeHttpContext>(), It.Is<string>(html => html.Contains("<html"))), Times.Once);
        responseAdapterMock.Verify(x => x.FinalizeAsync(It.IsAny<FakeHttpContext>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MatchingPathWrongMethod_CallsNextInstead()
    {
        var (middleware, responseAdapterMock) = CreateMiddleware("/spec-ui", "POST", "/spec-ui");
        var nextCalled = false;

        await middleware.HandleAsync(new FakeHttpContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        responseAdapterMock.Verify(x => x.SetBody(It.IsAny<FakeHttpContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NonMatchingPath_CallsNextInstead()
    {
        var (middleware, responseAdapterMock) = CreateMiddleware("/spec-ui", "GET", "/something-else");
        var nextCalled = false;

        await middleware.HandleAsync(new FakeHttpContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        responseAdapterMock.Verify(x => x.SetBody(It.IsAny<FakeHttpContext>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("/spec-ui", "spec-ui")]
    [InlineData("/spec-ui", "/SPEC-UI")]
    [InlineData("/spec-ui", "/spec-ui/")]
    [InlineData("spec-ui", "/spec-ui")]
    public async Task HandleAsync_PathNormalization_MatchesRegardlessOfCaseTrailingSlashOrLeadingSlash(
        string configuredPath, string requestPath)
    {
        var (middleware, responseAdapterMock) = CreateMiddleware(configuredPath, "GET", requestPath);
        var nextCalled = false;

        await middleware.HandleAsync(new FakeHttpContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        responseAdapterMock.Verify(x => x.FinalizeAsync(It.IsAny<FakeHttpContext>()), Times.Once);
    }
}
