using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Info;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Request;

namespace Benzene.Aws.Lambda.Sqs;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSqs(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<SqsMessageContext>, SqsMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<SqsMessageContext>, SqsMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SqsMessageContext>, SqsMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SqsMessageContext>, SqsMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<SqsMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsMessageContext, JsonSerializer>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sqs"));
        return services;
    }
}