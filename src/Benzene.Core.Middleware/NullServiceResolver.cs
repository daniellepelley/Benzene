using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

public class NullServiceResolver : IServiceResolver
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public T GetService<T>() where T : class
    {
        throw new NotImplementedException();
    }

    public T? TryGetService<T>() where T : class
    {
        return null;
    }
}