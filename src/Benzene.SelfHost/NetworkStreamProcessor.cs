using System.Text;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost;

public class NetworkStreamProcessor
{
    private readonly IEntryPointMiddlewareApplication<string, string> _middlewareApplication;

    public NetworkStreamProcessor(IEntryPointMiddlewareApplication<string, string> middlewareApplication)
    {
        _middlewareApplication = middlewareApplication;
    }

    public async Task<Task> HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        var data = await ReadAllAsync(stream, cancellationToken);
        return ProcessRequest(data, stream, cancellationToken);
    }

    private async Task ProcessRequest(string request, Stream stream, CancellationToken cancellationToken)
    {
        var response = await _middlewareApplication.HandleAsync(request);
        var responseData = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseData, 0, responseData.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        await stream.DisposeAsync();
    }

    private static async Task<string> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        var output = new List<byte>();
        var buffer = new byte[1024];
        var totalRead = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer, totalRead, buffer.Length, cancellationToken);
            if (read != 0)
            {
                output.AddRange(buffer.Take(read));
                break;
            }

            totalRead += read;
            output.AddRange(buffer);
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }
}
