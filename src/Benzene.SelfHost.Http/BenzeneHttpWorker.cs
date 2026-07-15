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
    /// Starts the accept loop on a background task and returns immediately - it does not wait for
    /// the loop to run to completion. Use <see cref="StopAsync"/> to signal shutdown and wait for
    /// in-flight requests to drain.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);
        var runToken = _linkedCts.Token;

        _runTask = Task.Run(async () =>
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_benzeneHttpConfig.Url);
            _httpListener.Start();

            var dispatcher = new BoundedConcurrentDispatcher<HttpListenerContext>(
                _benzeneHttpConfig.ConcurrentRequests,
                (httpContext, _) => _httpListenerApplication.HandleAsync(httpContext, _serviceResolverFactory),
                _logger);

            try
            {
                while (!runToken.IsCancellationRequested)
                {
                    try
                    {
                        var httpContext = await _httpListener.GetContextAsync().WaitAsync(runToken);
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
            _httpListener.Stop();
            _httpListener.Close();
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
