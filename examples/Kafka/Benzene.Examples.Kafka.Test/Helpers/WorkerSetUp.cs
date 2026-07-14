using Benzene.Examples.Kakfa;
using Benzene.HostedService;
using Microsoft.Extensions.Hosting;

namespace Benzene.Examples.Kafka.Test.Helpers;

public static class WorkerSetUp
{
    private static IHost _host;
    private static CancellationTokenSource _cancellationTokenSource;
    private static Thread _thread;

    public static void SetUp()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _thread = new Thread(async () =>
        {
            _host = Host.CreateDefaultBuilder()
                .UseBenzene<StartUp>()
                .Build();
            await _host.StartAsync(_cancellationTokenSource.Token);
        });
        _thread.Start();
    }

    public static async Task TearDownAsync()
    {
        await _host.StopAsync(_cancellationTokenSource.Token);
        _cancellationTokenSource.Cancel();
        // _thread.Interrupt();
        // _thread.Join();
    }
}
