namespace Benzene.Abstractions.Logging;

public interface IBenzeneLogAppender
{
    void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string message, params object[] args);
}
