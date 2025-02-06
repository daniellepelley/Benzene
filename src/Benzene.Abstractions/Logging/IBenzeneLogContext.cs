namespace Benzene.Abstractions.Logging;

public interface IBenzeneLogContext
{
    IDisposable Create(IDictionary<string, string> properties);
}
