using Grpc.Core;

namespace Benzene.Grpc.Test.Helpers;

/// <summary>A hand-rolled <see cref="IAsyncStreamReader{T}"/> that yields a fixed, in-memory sequence of items.</summary>
public class FakeAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IEnumerator<T> _items;

    public FakeAsyncStreamReader(IEnumerable<T> items)
    {
        _items = items.GetEnumerator();
    }

    public T Current { get; private set; } = default!;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_items.MoveNext())
        {
            return Task.FromResult(false);
        }

        Current = _items.Current;
        return Task.FromResult(true);
    }
}
