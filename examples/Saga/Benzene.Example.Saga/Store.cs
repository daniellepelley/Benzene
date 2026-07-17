namespace Benzene.Example.Saga;

/// <summary>
/// A trivial in-memory record store, so the example can show that a rolled-back saga leaves nothing
/// behind. Thread-safe because a stage's steps run concurrently.
/// </summary>
public class Store
{
    private readonly object _gate = new();
    private readonly List<string> _records = new();

    public void Add(string kind, string id)
    {
        lock (_gate) _records.Add($"{kind}:{id}");
    }

    public void Remove(string kind, string id)
    {
        lock (_gate) _records.Remove($"{kind}:{id}");
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate) return _records.ToArray();
    }
}
