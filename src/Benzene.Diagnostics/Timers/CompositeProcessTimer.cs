namespace Benzene.Diagnostics.Timers;

public sealed class CompositeProcessTimer : IProcessTimer
{
    private readonly IProcessTimer[] _scopes;

    public CompositeProcessTimer(IEnumerable<IProcessTimer> scopes)
    {
        _scopes = scopes.ToArray();
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
        {
            scope.Dispose();
        }
    }

    public void SetTag(string key, string value)
    {
        foreach (var scope in _scopes)
        {
            scope.SetTag(key, value);
        }
    }
}
