using Grpc.Core;

namespace Benzene.Grpc.Test.Helpers;

/// <summary>A hand-rolled <see cref="IServerStreamWriter{T}"/> that records every item written to it.</summary>
public class FakeServerStreamWriter<T> : IServerStreamWriter<T>
{
    public List<T> Written { get; } = new();

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Written.Add(message);
        return Task.CompletedTask;
    }
}
