using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Sns;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSns(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicGetter<SnsRecordContext>, SnsMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<SnsRecordContext>, SnsMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<SnsRecordContext>, SnsMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<SnsRecordContext>, SnsMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<SnsRecordContext>,
                MultiSerializerOptionsRequestMapper<SnsRecordContext, JsonSerializer>>();
       
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sns"));
        
        return services;
    }
}