using System.Linq;
using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.Correlation;
using Benzene.Core.Mappers;

namespace Benzene.Core.Logging;

public static class LogContextBuilderExtensions
{
    public static LogContextBuilder<TContext> WithApplication<TContext>(this LogContextBuilder<TContext> source)
    {
        return source.OnRequest("application", resolver =>
        {
            var applicationInfo = resolver.TryGetService<IApplicationInfo>();
            return applicationInfo?.Name;
        });
    }

    public static LogContextBuilder<TContext> WithTransport<TContext>(this LogContextBuilder<TContext> source)
    {
        return source.OnRequest("transport", resolver =>
        {
            var currentTransport = resolver.GetService<ICurrentTransport>();
            return currentTransport.Name;
        });
    }

    public static LogContextBuilder<TContext> WithHeaders<TContext>(this LogContextBuilder<TContext> source, params string[] headers)
    {
        return source.OnRequest((resolver, context) =>
        {
            var messageMapper = resolver.Resolve<IMessageHeadersMapper<TContext>>();

            var dictionary = headers.ToDictionary(header => header,
                header => messageMapper.GetHeader(context, header));

            return dictionary;
        });
    }

    public static LogContextBuilder<TContext> WithCorrelationId<TContext>(this LogContextBuilder<TContext> source)
    {
        source.Register(x => x.AddCorrelationId());
        return source.OnRequest("correlationId", resolver =>
        {
            var correlationId = resolver.GetService<ICorrelationId>();
            return correlationId.Get();
        });
    }

    public static LogContextBuilder<TContext> WithTopic<TContext>(this LogContextBuilder<TContext> source)
    {
        return source.OnRequest("topic", GetTopic);
    }

    // public static LogContextBuilder<TContext> WithResponseStatus<TContext>(this LogContextBuilder<TContext> source)
    // {
    //     return source.OnResponse("status", (resolver, context) =>
    //     {
    //         if (context.MessageResult?.Status != null)
    //         {
    //             return context.MessageResult.Status;
    //         }
    //
    //         return null;
    //     });
    // }

    // public static LogContextBuilder<TContext> WithResponseError<TContext>(this LogContextBuilder<TContext> source)
    //     where TContext 
    // {
    //     return source.OnResponse("error", (resolver, context) =>
    //     {
    //         if (context.MessageResult?.Errors != null && context.MessageResult.Errors.Any())
    //         {
    //             return context.MessageResult.Errors.First();
    //         }
    //         return null;
    //     });
    // }

    private static string GetTopic<TContext>(IServiceResolver resolver, TContext context)
    {
        var mapper = resolver.TryGetService<IMessageMapper<TContext>>();

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
