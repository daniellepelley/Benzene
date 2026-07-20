using System.Net;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.SelfHost;
using Microsoft.Extensions.Logging;

namespace Benzene.SelfHost.Http;

public class BenzeneHttpWorker : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly HttpListenerApplication _httpListenerApplication;
    private readonly BenzeneHttpConfig _benzeneHttpConfig;
    private readonly ILogger<BenzeneHttpWorker> _logger;
    private readonly CancellationTokenSource _stoppingCts = new();
    private HttpListener? _httpListener;
    private Task? _runTask;
    private CancellationTokenSource? _linkedCts;

    public BenzeneHttpWorker(IServiceResolverFactory serviceResolverFactory,
        HttpListenerApplication httpListenerApplication, BenzeneHttpConfig benzeneHttpConfig,
        ILogger<BenzeneHttpWorker> logger)
    {
        _benzeneHttpConfig = benzeneHttpConfig;
        _httpListenerApplication = httpListenerApplication;
        _serviceResolverFactory = serviceResolverFactory;
        _logger = logger;
    }

    /// <summary>
    /// Binds and starts the listener, then runs the accept loop on a background task and returns.
    /// The listener is bound and started <em>synchronously</em> so a bind failure (port in use,
    /// access denied) propagates out of this method and fails host startup loudly, rather than
    /// faulting an unobserved background task that would only surface at <see cref="StopAsync"/>.
    /// Use <see cref="StopAsync"/> to signal shutdown and wait for in-flight requests to drain.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);
        var runToken = _linkedCts.Token;

        // Bind + start on the calling thread: an HttpListenerException here (e.g. the prefix is
        // already in use or requires elevation) propagates to the host instead of being swallowed.
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(_benzeneHttpConfig.Url);
        httpListener.Start();
        _httpListener = httpListener;

        _runTask = Task.Run(async () =>
        {
            var dispatcher = new BoundedConcurrentDispatcher<HttpListenerContext>(
                _benzeneHttpConfig.ConcurrentRequests,
                (httpContext, _) => _httpListenerApplication.HandleAsync(httpContext, _serviceResolverFactory, runToken),
                _logger);

            try
            {
                while (!runToken.IsCancellationRequested)
                {
                    try
                    {
                        var httpContext = await httpListener.GetContextAsync().WaitAsync(runToken);
                        await dispatcher.EnqueueAsync(httpContext, runToken);
                    }
                    catch (HttpListenerException e)
                    {
                        _logger.LogError(e, "HTTP listener error");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown - fall through to drain and close below.
            }

            await dispatcher.DrainAsync(_benzeneHttpConfig.DrainTimeout);
            httpListener.Stop();
            httpListener.Close();
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Signals the accept loop to stop, then waits for it to drain in-flight requests
    /// (up to <see cref="BenzeneHttpConfig.DrainTimeout"/>) and close the listener.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();

        if (_runTask != null)
        {
            await _runTask;
        }
    }

    public void Dispose()
    {
        _stoppingCts.Dispose();
        _linkedCts?.Dispose();
        _serviceResolverFactory.Dispose();
    }
}
