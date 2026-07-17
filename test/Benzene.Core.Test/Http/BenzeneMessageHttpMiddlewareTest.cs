using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Http;
using Benzene.Http.BenzeneMessage;
using Benzene.Results;
using Moq;
using Xunit;

namespace Benzene.Test.Http;

public class BenzeneMessageHttpMiddlewareTest
{
    // Public (not private) because Moq needs to build a dynamic proxy for the adapter interfaces
    // closed over this type, which requires the type argument to be accessible.
    public class FakeHttpContext : IHttpContext
    {
    }

    private class FakeSetCurrentTransport : ISetCurrentTransport
    {
        public void SetTransport(string transport)
        {
        }
    }

    private class FakeServiceResolver : IServiceResolver
    {
        public void Dispose()
        {
        }

        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IServiceResolverFactory))
            {
                return (T)(object)new FakeServiceResolverFactory(this);
            }

            if (typeof(T) == typeof(ISetCurrentTransport))
            {
                return (T)(object)new FakeSetCurrentTransport();
            }

            throw new InvalidOperationException($"No service registered for {typeof(T).Name}");
        }

        public T? TryGetService<T>() where T : class => null;

        public IEnumerable<T> GetServices<T>() where T : class => Array.Empty<T>();
    }

    private class FakeServiceResolverFactory : IServiceResolverFactory
    {
        private readonly IServiceResolver _serviceResolver;

        public FakeServiceResolverFactory(IServiceResolver serviceResolver)
        {
            _serviceResolver = serviceResolver;
        }

        public IServiceResolver CreateScope() => _serviceResolver;

        public void Dispose()
        {
        }
    }

    private static IMiddlewarePipeline<BenzeneMessageContext> CreateEchoPipeline()
    {
        return new MiddlewarePipeline<BenzeneMessageContext>(new Func<IServiceResolver, IMiddleware<BenzeneMessageContext>>[]
        {
            _ => new FuncWrapperMiddleware<BenzeneMessageContext>((context, _) =>
            {
                context.BenzeneMessageResponse.StatusCode = BenzeneResultStatus.Ok;
                context.BenzeneMessageResponse.Body = $"{{\"echoTopic\":\"{context.BenzeneMessageRequest.Topic}\"}}";
                return Task.CompletedTask;
            })
        });
    }

    private static (BenzeneMessageHttpMiddleware<FakeHttpContext> Middleware,
        Mock<IBenzeneResponseAdapter<FakeHttpContext>> ResponseAdapterMock,
        List<string> BodiesWritten)
        CreateMiddleware(string method, string requestPath, string? requestBody,
            BenzeneMessageHttpOptions? options = null)
    {
        var requestAdapterMock = new Mock<IHttpRequestAdapter<FakeHttpContext>>();
        requestAdapterMock.Setup(x => x.Map(It.IsAny<FakeHttpContext>()))
            .Returns(new HttpRequest { Method = method, Path = requestPath });

        var bodyGetterMock = new Mock<IMessageBodyGetter<FakeHttpContext>>();
        bodyGetterMock.Setup(x => x.GetBody(It.IsAny<FakeHttpContext>())).Returns(requestBody);

        var bodiesWritten = new List<string>();
        var responseAdapterMock = new Mock<IBenzeneResponseAdapter<FakeHttpContext>>();
        responseAdapterMock.Setup(x => x.SetBody(It.IsAny<FakeHttpContext>(), It.IsAny<string>()))
            .Callback<FakeHttpContext, string>((_, body) => bodiesWritten.Add(body));

        var middleware = new BenzeneMessageHttpMiddleware<FakeHttpContext>(
            options ?? new BenzeneMessageHttpOptions(),
            CreateEchoPipeline(),
            new FakeServiceResolver(),
            requestAdapterMock.Object,
            bodyGetterMock.Object,
            responseAdapterMock.Object,
            new DefaultHttpStatusCodeMapper());

        return (middleware, responseAdapterMock, bodiesWritten);
    }

    private const string Envelope = "{\"topic\":\"example\",\"headers\":{},\"body\":\"{}\"}";

    [Fact]
    public async Task HandleAsync_PostToMatchingPath_DispatchesAndShortCircuits()
    {
        var (middleware, responseAdapterMock, bodies) = CreateMiddleware("POST", "/benzene-message", Envelope);
        var nextCalled = false;

        await middleware.HandleAsync(new FakeHttpContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        responseAdapterMock.Verify(x => x.SetStatusCode(It.IsAny<FakeHttpContext>(), "200"), Times.Once);
        responseAdapterMock.Verify(x => x.SetContentType(It.IsAny<FakeHttpContext>(), "application/json; charset=utf-8"), Times.Once);
        responseAdapterMock.Verify(x => x.FinalizeAsync(It.IsAny<FakeHttpContext>()), Times.Once);
        var body = Assert.Single(bodies);
        Assert.Contains("\"statusCode\":\"Ok\"", body);
        Assert.Contains("echoTopic", body);
    }

    [Theory]
    [InlineData("GET", "/benzene-message")]
    [InlineData("PUT", "/benzene-message")]
    [InlineData("POST", "/something-else")]
    public async Task HandleAsync_NonMatchingRequest_CallsNextInstead(string method, string path)
    {
        var (middleware, responseAdapterMock, _) = CreateMiddleware(method, path, Envelope);
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
    [InlineData("not-json-at-all")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{\"headers\":{}}")]
    public async Task HandleAsync_InvalidOrTopiclessEnvelope_RespondsBadRequest(string? requestBody)
    {
        var (middleware, responseAdapterMock, bodies) = CreateMiddleware("POST", "/benzene-message", requestBody);

        await middleware.HandleAsync(new FakeHttpContext(), () => Task.CompletedTask);

        responseAdapterMock.Verify(x => x.SetStatusCode(It.IsAny<FakeHttpContext>(), "400"), Times.Once);
        var body = Assert.Single(bodies);
        Assert.Contains(BenzeneResultStatus.BadRequest, body);
    }

    [Fact]
    public async Task HandleAsync_TopicRejectedByFilter_RespondsNotFound()
    {
        var options = new BenzeneMessageHttpOptions { TopicFilter = topic => topic != "example" };
        var (middleware, responseAdapterMock, bodies) = CreateMiddleware("POST", "/benzene-message", Envelope, options);

        await middleware.HandleAsync(new FakeHttpContext(), () => Task.CompletedTask);

        responseAdapterMock.Verify(x => x.SetStatusCode(It.IsAny<FakeHttpContext>(), "404"), Times.Once);
        var body = Assert.Single(bodies);
        Assert.Contains(BenzeneResultStatus.NotFound, body);
        Assert.DoesNotContain("echoTopic", body);
    }

    [Theory]
    [InlineData("/benzene-message", "benzene-message")]
    [InlineData("/benzene-message", "/BENZENE-MESSAGE")]
    [InlineData("/benzene-message", "/benzene-message/")]
    [InlineData("benzene-message", "/benzene-message")]
    [InlineData("/custom-path", "/custom-path")]
    public async Task HandleAsync_PathNormalization_MatchesRegardlessOfCaseTrailingSlashOrLeadingSlash(
        string configuredPath, string requestPath)
    {
        var options = new BenzeneMessageHttpOptions { Path = configuredPath };
        var (middleware, responseAdapterMock, _) = CreateMiddleware("POST", requestPath, Envelope, options);
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
