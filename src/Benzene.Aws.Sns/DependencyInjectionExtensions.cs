﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.DI;
using Benzene.Core.Info;
using Benzene.Core.Request;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Sns;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddSns(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.AddScoped<IMessageTopicMapper<SnsRecordContext>, SnsMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<SnsRecordContext>, SnsMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<SnsRecordContext>, SnsMessageBodyMapper>();
        services
            .AddScoped<IRequestMapper<SnsRecordContext>,
                MultiSerializerOptionsRequestMapper<SnsRecordContext, JsonSerializer>>();
       
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("sns"));
        
        return services;
    }
}


