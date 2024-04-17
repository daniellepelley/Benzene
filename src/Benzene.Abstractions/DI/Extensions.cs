namespace Benzene.Abstractions.DI;

public static class Extensions
{
    public static T Resolve<T>(this IServiceResolver source) where T : class
    {
        return source.GetService<T>();
    }

    public static T? TryResolve<T>(this IServiceResolver source) where T : class
    {
        return source.TryGetService<T>();
    }
}
