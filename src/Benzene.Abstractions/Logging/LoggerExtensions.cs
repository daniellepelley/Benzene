namespace Benzene.Abstractions.Logging;

public static class LoggerExtensions
{
    public static void LogDebug(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Debug, exception, message, args);
    }

    public static void LogDebug(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Debug, message, args);
    }

    public static void LogTrace(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Trace, exception, message, args);
    }

    public static void LogTrace(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Trace, message, args);
    }

    public static void LogInformation(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Information, exception, message, args);
    }

    public static void LogInformation(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Information, message, args);
    }

    public static void LogWarning(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Warning, exception, message, args);
    }

    public static void LogWarning(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Warning, message, args);
    }

    public static void LogError(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Error, exception, message, args);
    }

    public static void LogError(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Error, message, args);
    }

    public static void LogCritical(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Critical, exception, message, args);
    }

    public static void LogCritical(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Critical, message, args);
    }

    public static void Log(this IBenzeneLogger benzeneLogger, BenzeneLogLevel benzeneLogLevel, string message, params object[] args)
    {
        benzeneLogger.Log(benzeneLogLevel, null, message, args);
    }
}
