using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.Info;
using Benzene.Core.Request;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Sqs;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSqs(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<SqsMessageContext>, SqsMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<SqsMessageContext>, SqsMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<SqsMessageContext>, SqsMessageBodyMapper>();
        services
            .AddScoped<IRequestMapper<SqsMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsMessageContext, JsonSerializer>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sqs"));
        return services;
    }

    public static IBenzeneServiceContainer AddSqsConsumer(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<ISqsClientFactory, SqsClientFactory>();
        services.AddScoped<IMessageTopicMapper<SqsConsumerMessageContext>, SqsConsumerMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<SqsConsumerMessageContext>, SqsConsumerMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<SqsConsumerMessageContext>, SqsConsumerMessageBodyMapper>();
        services
            .AddScoped<IRequestMapper<SqsConsumerMessageContext>,
                MultiSerializerOptionsRequestMapper<SqsConsumerMessageContext, JsonSerializer>>();

        return services;
    }

}


