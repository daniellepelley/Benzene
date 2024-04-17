namespace Benzene.Abstractions.DI;

public interface IServiceResolverFactory : IDisposable
{
    IServiceResolver CreateScope();
}