using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Logging;

public static class LogContextBuilderExtensions
{
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, string value)
    {
        return source.OnRequest(key, (_, _) => value);
    }

    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.OnRequest(key, (resolver, _) => valueAction(resolver));
    }

    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.OnRequest((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }

    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.OnRequest((_, _) => dictionary);
    }

    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.OnRequest((resolver, _) => dictionaryAction(resolver));
    }

    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, string value)
    {
        return source.OnResponse(key, (_, _) => value);
    }

    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.OnResponse(key, (resolver, _) => valueAction(resolver));
    }
    
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.OnResponse((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }
    
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.OnResponse((_, _) => dictionary);
    }
    
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.OnResponse((resolver, _) => dictionaryAction(resolver));
    }
}