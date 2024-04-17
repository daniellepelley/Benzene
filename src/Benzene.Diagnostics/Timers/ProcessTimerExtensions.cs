namespace Benzene.Diagnostics.Timers;

public static class ProcessTimerExtensions
{
    public static T TimeMethod<T>(this IProcessTimerFactory source, string timerName, Func<T> func)
    {
        using (source.Create(timerName))
        {
            return func();
        }
    }

    public static async Task<T> TimeMethodAsync<T>(this IProcessTimerFactory source, string timerName, Func<Task<T>> func)
    {
        using (source.Create(timerName))
        {
            return await func();
        }
    }
}
