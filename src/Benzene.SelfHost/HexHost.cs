using System.Net;
using System.Net.Sockets;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost;

public class BenzeneHost 
{
    private readonly List<Task> _tasks = new();
    private readonly object _lock = new();
    private readonly NetworkStreamProcessor _processor;

    public BenzeneHost(IEntryPointMiddlewareApplication<string, string> middlewareApplication)
    {
        _processor = new NetworkStreamProcessor(middlewareApplication);
    }

    public async Task StartAsync(int port, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            CleanUpTasks();
            using var client = await listener.AcceptTcpClientAsync(cancellationToken);
            var task = await _processor.HandleClientAsync(client.GetStream(), cancellationToken);
            AddTask(task);
        }
        listener.Stop();
    }

    private void AddTask(Task task)
    {
        lock (_lock)
        {
            _tasks.Add(task);
        }
    }

    private void CleanUpTasks()
    {
        lock (_lock)
        {
            foreach (var task in _tasks.Where(task => task.IsCompleted || task.IsFaulted || task.IsCanceled).ToArray())
            {
                _tasks.Remove(task);
            }
        }
    }
}
