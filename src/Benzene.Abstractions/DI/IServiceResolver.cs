namespace Benzene.Abstractions.DI;

public interface IServiceResolver : IDisposable
{
    T GetService<T>() where T : class;
    T? TryGetService<T>() where T : class;
}