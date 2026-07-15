using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.SelfHost.Http;

/// <summary>
/// End-to-end coverage for the previously-untested self-hosted HTTP path (see
/// <c>Benzene.SelfHost.Http/CLAUDE.md</c>): <see cref="SelfHostHttpContext"/> wraps a sealed,
/// constructor-less <see cref="HttpListenerContext"/>, so it can't be exercised with a fake context
/// object - this binds a real <see cref="HttpListener"/> to a free loopback port via
/// <see cref="BenzeneHttpWorker"/> and drives it with a real <see cref="HttpClient"/> instead.
/// </summary>
public class BenzeneHttpWorkerTest
{
    /// <summary>
    /// Captures the last exception logged via <see cref="ILogger{TCategoryName}"/> so a hung/failed
    /// request can report the real cause instead of just "the client timed out" - per-request
    /// exceptions in <c>BoundedConcurrentDispatcher</c>'s consumer loop are caught and logged, not
    /// rethrown, so they'd otherwise be invisible to the test.
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public Exception? LastException { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception != null)
            {
                LastException = exception;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<HttpResponseMessage> PollUntilRespondingAsync(HttpClient httpClient, string url, CapturingLogger<BenzeneHttpWorker> logger)
    {
        // BenzeneHttpWorker.StartAsync kicks off the accept loop on a background task and returns
        // immediately (by design - see its own doc comment), so the listener may not have called
        // HttpListener.Start() yet by the time this runs. Retry briefly instead of racing it.
        Exception? lastException = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                return await httpClient.GetAsync(url);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
                if (logger.LastException != null)
                {
                    break;
                }
                await Task.Delay(50);
            }
        }

        throw new TimeoutException(
            $"Server never responded. Last logged server-side exception: {logger.LastException}", lastException);
    }

    [Fact]
    public async Task StartAsync_LivenessCheck_RespondsOverRealHttp()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig
        {
            Url = $"http://127.0.0.1:{port}/",
            ConcurrentRequests = 4
        };
        var capturingLogger = new CapturingLogger<BenzeneHttpWorker>();

        var worker = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger))
            .Configure(app => app.UseHttp(config, pipeline => pipeline.UseLivenessCheck()))
            .Build();

        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await PollUntilRespondingAsync(httpClient, $"http://127.0.0.1:{port}/livez", capturingLogger);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_UnmatchedPath_FallsThroughToMissingTopic()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig
        {
            Url = $"http://127.0.0.1:{port}/",
            ConcurrentRequests = 4
        };
        var capturingLogger = new CapturingLogger<BenzeneHttpWorker>();

        var worker = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger))
            .Configure(app => app.UseHttp(config, pipeline => pipeline
                .UseLivenessCheck()
                .UseMessageHandlers()))
            .Build();

        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            // /livez only matches GET - a request for an unregistered path falls through the health
            // check middleware into UseMessageHandlers, which has no handler for it and no topic
            // header set, so it resolves to the router's "missing topic" validation error rather than
            // ever reaching a handler.
            var response = await PollUntilRespondingAsync(httpClient, $"http://127.0.0.1:{port}/not-a-real-path", capturingLogger);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopAsync_DrainsInFlightRequestBeforeClosingListener()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig
        {
            Url = $"http://127.0.0.1:{port}/",
            ConcurrentRequests = 4,
            DrainTimeout = TimeSpan.FromSeconds(5)
        };
        var capturingLogger = new CapturingLogger<BenzeneHttpWorker>();

        var worker = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger))
            .Configure(app => app.UseHttp(config, pipeline => pipeline.UseLivenessCheck()))
            .Build();

        await worker.StartAsync(CancellationToken.None);
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var firstResponse = await PollUntilRespondingAsync(httpClient, $"http://127.0.0.1:{port}/livez", capturingLogger);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        await worker.StopAsync(CancellationToken.None);

        // The listener is closed after StopAsync returns - a further request must fail to connect
        // rather than hang or succeed, proving the accept loop actually stopped.
        await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
            httpClient.GetAsync($"http://127.0.0.1:{port}/livez"));
    }
}
