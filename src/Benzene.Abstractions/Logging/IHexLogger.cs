namespace Benzene.Abstractions.Logging;

public interface IBenzeneLogger
{
    void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string message, params object[] args);
}
