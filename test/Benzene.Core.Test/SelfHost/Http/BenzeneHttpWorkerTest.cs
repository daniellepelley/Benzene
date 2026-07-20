using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
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
    public async Task StartAsync_PrefixAlreadyBound_ThrowsFromStartAsync_NotSilently()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig { Url = $"http://127.0.0.1:{port}/", ConcurrentRequests = 4 };

        var worker1 = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services.AddLogging().AddSingleton<ILogger<BenzeneHttpWorker>>(new CapturingLogger<BenzeneHttpWorker>()))
            .Configure(app => app.UseHttp(config, pipeline => pipeline.UseLivenessCheck()))
            .Build();

        await worker1.StartAsync(CancellationToken.None);
        try
        {
            var worker2 = new InlineSelfHostedStartUp()
                .ConfigureServices(services => services.AddLogging().AddSingleton<ILogger<BenzeneHttpWorker>>(new CapturingLogger<BenzeneHttpWorker>()))
                .Configure(app => app.UseHttp(config, pipeline => pipeline.UseLivenessCheck()))
                .Build();

            // The second worker can't bind the already-bound prefix; the failure must propagate out of
            // StartAsync (host startup fails loudly) rather than faulting an unobserved background task.
            await Assert.ThrowsAnyAsync<HttpListenerException>(() => worker2.StartAsync(CancellationToken.None));
        }
        finally
        {
            await worker1.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_RequestBodyExceedsLimit_IsRejectedByRequestBodyTooLargeException()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig
        {
            Url = $"http://127.0.0.1:{port}/",
            ConcurrentRequests = 4,
            MaxRequestBodyBytes = 16
        };
        var capturingLogger = new CapturingLogger<BenzeneHttpWorker>();

        var worker = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger)
                .UsingBenzene(x => x.AddBenzene()))
            .Configure(app => app.UseHttp(config, pipeline => pipeline.UseLivenessCheck()))
            .Build();

        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            try
            {
                // A body far larger than the 16-byte cap: the server rejects it while reading, so the
                // client sees a reset/error rather than the server buffering it all.
                await httpClient.PostAsync($"http://127.0.0.1:{port}/livez", new StringContent(new string('x', 4096)));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Expected - the server aborted the oversized request.
            }

            for (var attempt = 0; attempt < 40 && capturingLogger.LastException == null; attempt++)
            {
                await Task.Delay(50);
            }

            Assert.IsType<RequestBodyTooLargeException>(capturingLogger.LastException);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Post_BinaryBody_IsBufferedAndServedVerbatimByTheBytesGetter()
    {
        var port = GetFreeTcpPort();
        var config = new BenzeneHttpConfig { Url = $"http://127.0.0.1:{port}/", ConcurrentRequests = 4 };
        var capturingLogger = new CapturingLogger<BenzeneHttpWorker>();

        // Bytes that are NOT valid standalone UTF-8 (0xFF, 0xFE, lone 0x80): a string round-trip would
        // corrupt them, so an exact round-trip proves the raw-byte request path is used end to end.
        var binary = new byte[] { 0x00, 0x01, 0x02, 0x89, 0x50, 0x4E, 0x47, 0xFF, 0xFE, 0x80, 0x7F };
        byte[]? captured = null;

        var worker = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .AddLogging()
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger)
                .UsingBenzene(x => x.AddBenzene()))
            .Configure(app => app.UseHttp(config, pipeline => pipeline
                .Use(resolver => new Benzene.Core.Middleware.FuncWrapperMiddleware<SelfHostHttpContext>(
                    "CaptureBytes", (context, next) =>
                    {
                        var bytesGetter = resolver.GetService<Benzene.Abstractions.Messages.Mappers.IMessageBodyBytesGetter<SelfHostHttpContext>>();
                        captured = bytesGetter.GetBodyBytes(context).ToArray();
                        return next();
                    }))
                .UseMessageHandlers()))
            .Build();

        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            // Poll until the listener is up, then POST the binary body.
            for (var attempt = 0; attempt < 20 && captured == null; attempt++)
            {
                try
                {
                    var content = new ByteArrayContent(binary);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    await httpClient.PostAsync($"http://127.0.0.1:{port}/capture", content);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    await Task.Delay(50);
                }
            }

            Assert.NotNull(captured);
            Assert.Equal(binary, captured);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_LivenessCheck_WithQueryString_StillMatches()
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
            // A query string must not break path matching: HttpRequest.Path is the path only. The
            // adapter used RawUrl (which includes "?probe=1"), so "/livez?probe=1" failed to match
            // "/livez" and fell through undispatched.
            var response = await PollUntilRespondingAsync(httpClient, $"http://127.0.0.1:{port}/livez?probe=1", capturingLogger);

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
                .AddSingleton<ILogger<BenzeneHttpWorker>>(capturingLogger)
                .UsingBenzene(x => x.AddBenzene()))
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
    public async Task StopAsync_AfterHandlingARequest_CompletesWithoutHanging()
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
        using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
        {
            var firstResponse = await PollUntilRespondingAsync(httpClient, $"http://127.0.0.1:{port}/livez", capturingLogger);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        }

        // Proves the drain-then-close logic in StartAsync's background loop actually runs to
        // completion (rather than the accept loop wedging on a pending GetContextAsync, or the
        // dispatcher's drain never observing the earlier request as finished) - bounded so a hang
        // fails the test instead of the whole run.
        var stopTask = worker.StopAsync(CancellationToken.None);
        var completedTask = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(stopTask, completedTask);
    }
}
