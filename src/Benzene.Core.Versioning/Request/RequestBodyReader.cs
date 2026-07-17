using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Core.Versioning.Request;

/// <summary>
/// Invokes <see cref="IRequestMapper{TContext}.GetBody{TRequest}"/> for a request type only known at
/// runtime (the incoming schema version's CLR type, from <c>ISchemaCaster.FromType</c>). The generic
/// method is closed over that type once via a compiled delegate and cached per type, so the per-message
/// path never pays reflection cost after the first message of each version.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type.</typeparam>
internal static class RequestBodyReader<TContext>
{
    private static readonly MethodInfo GetBodyMethod =
        typeof(IRequestMapper<TContext>).GetMethod(nameof(IRequestMapper<TContext>.GetBody))!;

    private static readonly ConcurrentDictionary<Type, Func<IRequestMapper<TContext>, TContext, object?>> Cache = new();

    public static object? Read(IRequestMapper<TContext> mapper, TContext context, Type requestType)
    {
        var reader = Cache.GetOrAdd(requestType, BuildReader);
        return reader(mapper, context);
    }

    private static Func<IRequestMapper<TContext>, TContext, object?> BuildReader(Type requestType)
    {
        // (mapper, context) => (object?)mapper.GetBody<requestType>(context)
        var mapperParam = Expression.Parameter(typeof(IRequestMapper<TContext>), "mapper");
        var contextParam = Expression.Parameter(typeof(TContext), "context");

        var call = Expression.Call(mapperParam, GetBodyMethod.MakeGenericMethod(requestType), contextParam);
        var body = Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<IRequestMapper<TContext>, TContext, object?>>(body, mapperParam, contextParam).Compile();
    }
}
