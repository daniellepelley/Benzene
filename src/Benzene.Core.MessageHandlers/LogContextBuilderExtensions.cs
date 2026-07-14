using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Fluent <see cref="ILogContextBuilder{TContext}"/> extensions that enrich the log scope with
/// message-handling metadata (application name, transport, topic, selected headers).
/// </summary>
public static class LogContextBuilderExtensions
{
    /// <summary>
    /// Adds the registered <see cref="IApplicationInfo"/>'s name to the log scope, under the key
    /// <c>"application"</c>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="source">The log context builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILogContextBuilder<TContext> WithApplication<TContext>(this ILogContextBuilder<TContext> source)
    {
        return source.OnRequest("application", resolver =>
        {
            var applicationInfo = resolver.TryGetService<IApplicationInfo>();
            return applicationInfo?.Name;
        });
    }

    /// <summary>
    /// Adds the current transport's name (see <see cref="ICurrentTransport"/>) to the log scope, under
    /// the key <c>"transport"</c>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="source">The log context builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILogContextBuilder<TContext> WithTransport<TContext>(this ILogContextBuilder<TContext> source)
    {
        return source.OnRequest("transport", resolver =>
        {
            var currentTransport = resolver.GetService<ICurrentTransport>();
            return currentTransport.Name;
        });
    }

    /// <summary>
    /// Adds the given headers of the incoming message to the log scope, keyed by header name.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="source">The log context builder to configure.</param>
    /// <param name="headers">The header names to include.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILogContextBuilder<TContext> WithHeaders<TContext>(this ILogContextBuilder<TContext> source, params string[] headers)
    {
        return source.OnRequest((resolver, context) =>
        {
            var messageMapper = resolver.Resolve<IMessageHeadersGetter<TContext>>();

            var dictionary = headers.ToDictionary(header => header,
                header => messageMapper.GetHeader(context, header));

            return dictionary;
        });
    }


    /// <summary>
    /// Adds the incoming message's topic to the log scope, under the key <c>"topic"</c> (or
    /// <c>"&lt;missing&gt;"</c> if no <see cref="IMessageGetter{TContext}"/> is registered, or the
    /// message has no topic).
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="source">The log context builder to configure.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILogContextBuilder<TContext> WithTopic<TContext>(this ILogContextBuilder<TContext> source)
    {
        return source.OnRequest("topic", GetTopic);
    }

    private static string GetTopic<TContext>(IServiceResolver resolver, TContext context)
    {
        var mapper = resolver.TryGetService<IMessageGetter<TContext>>();

        if (mapper == null)
        {
            return "<missing>";
        }

        var messageTopic = mapper.GetTopic(context);

        return string.IsNullOrEmpty(messageTopic.Id)
            ? "<missing>"
            : messageTopic.Id;
    }
}
