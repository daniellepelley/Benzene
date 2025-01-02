using System.Net;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;

namespace Benzene.SelfHost.Http;

public class BenzeneHttpWorker : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private HttpListener _httpListener;
    private readonly HttpListenerApplication _httpListenerApplication;
    private readonly BenzeneHttpConfig _benzeneHttpConfig;

    public BenzeneHttpWorker(IServiceResolverFactory serviceResolverFactory,
        HttpListenerApplication httpListenerApplication, BenzeneHttpConfig benzeneHttpConfig)
    {
        _benzeneHttpConfig = benzeneHttpConfig;
        _httpListenerApplication = httpListenerApplication;
        _serviceResolverFactory = serviceResolverFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_benzeneHttpConfig.Url);
            _httpListener.Start();
            var semaphore = new SemaphoreSlim(_benzeneHttpConfig.ConcurrentRequests);
            
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    
                    try
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        var httpContext = await _httpListener.GetContextAsync();
                        _httpListenerApplication.HandleAsync(httpContext, _serviceResolverFactory)
                            .ContinueWith(_ => semaphore.Release());
                    }
                    catch (Exception ex)
                    {
                        semaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _httpListener.Close();
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _httpListener?.Stop();
        _httpListener?.Close();
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _serviceResolverFactory.Dispose();
    }
}