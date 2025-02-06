using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

public static class LogContextBuilderExtensions
{
    public static ILogContextBuilder<TContext> WithApplication<TContext>(this ILogContextBuilder<TContext> source)
    {
        return source.OnRequest("application", resolver =>
        {
            var applicationInfo = resolver.TryGetService<IApplicationInfo>();
            return applicationInfo?.Name;
        });
    }

    public static ILogContextBuilder<TContext> WithTransport<TContext>(this ILogContextBuilder<TContext> source)
    {
        return source.OnRequest("transport", resolver =>
        {
            var currentTransport = resolver.GetService<ICurrentTransport>();
            return currentTransport.Name;
        });
    }

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