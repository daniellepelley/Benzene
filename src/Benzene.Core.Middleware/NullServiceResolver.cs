using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

public class NullServiceResolver : IServiceResolver
{
    public void Dispose()
    {
    }

    public T GetService<T>() where T : class
    {
        return default!;
    }

    public T? TryGetService<T>() where T : class
    {
        return null;
    }
}