using Benzene.Examples.Kakfa;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.Examples.Kafka.Test.Helpers;

public static class WorkerSetUp
{
    private static Worker _worker;
    private static CancellationTokenSource _cancellationTokenSource;
    private static Thread _thread;

    public static void SetUp()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _thread = new Thread(async () =>
        {
            _worker = new Worker(NullLogger<Worker>.Instance);
            await _worker.StartAsync(_cancellationTokenSource.Token);
        });
        _thread.Start();
    }

    public static async Task TearDownAsync()
    {
        await _worker.StopAsync(_cancellationTokenSource.Token);
        _cancellationTokenSource.Cancel();
        _worker.Dispose();
        // _thread.Interrupt();
        // _thread.Join();
    }
}