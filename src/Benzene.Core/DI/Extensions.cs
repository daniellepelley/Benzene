using System;
using System.Linq;
using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Serialization;
using Benzene.Abstractions.Validation;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Helper;
using Benzene.Core.Info;
using Benzene.Core.Logging;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.Core.Validation;
using Utils = Benzene.Core.Helper.Utils;

namespace Benzene.Core.DI;

public static class Extensions
{
    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        params Assembly[] assemblies)
    {
        var types = Utils.GetAllTypes(assemblies).ToArray();
        return services.AddMessageHandlers(types);
    }

    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        Type[] types)
    {
        services.AddMessageHandlers2(types);
        services.AddContextItems();
        return services;
    }

    public static IBenzeneServiceContainer AddBenzeneMessage(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IRequestMapper<BenzeneMessageContext>, MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>>();
        services.TryAddScoped<IMessageMapper<BenzeneMessageContext>, BenzeneMessageMapper>();
        services.TryAddScoped<IMessageBodyMapper<BenzeneMessageContext>, BenzeneMessageMapper>();
        services.TryAddScoped<IMessageTopicMapper<BenzeneMessageContext>, BenzeneMessageMapper>();
        services.TryAddScoped<IMessageHeadersMapper<BenzeneMessageContext>, BenzeneMessageMapper>();
        services.TryAddScoped<IBenzeneResponseAdapter<BenzeneMessageContext>, BenzeneMessageResponseAdapter>();
    
        services.TryAddScoped<IResponseHandler<BenzeneMessageContext>, ResponseBodyHandler<BenzeneMessageContext>>();
        services.AddScoped<IResponseHandler<BenzeneMessageContext>, DefaultResponseStatusHandler<BenzeneMessageContext>>();
        services.TryAddScoped<IResponsePayloadMapper<BenzeneMessageContext>, DefaultResponsePayloadMapper<BenzeneMessageContext>>();
    
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("direct"));
    
        return services;
    }


    public static IBenzeneServiceContainer AddContextItems(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped(typeof(IMessageMapper<>), typeof(MessageMapper<>));
        services.TryAddScoped(typeof(IResponsePayloadMapper<>), typeof(DefaultResponsePayloadMapper<>));
        services.TryAddScoped(typeof(IResponseHandlerContainer<>), typeof(ResponseHandlerContainer<>));
        services.TryAddScoped(typeof(ResponseMiddleware<>));
        services.TryAddScoped(typeof(ResponseIfHandledMiddleware<>));
        services.TryAddScoped(typeof(JsonSerializationResponseHandler<>));
    
        services.TryAddScoped(typeof(IRequestMapper<>), typeof(JsonDefaultMultiSerializerOptionsRequestMapper<>));
        return services;
    }

    public static IBenzeneServiceContainer SetApplicationInfo(this IBenzeneServiceContainer services,
        string name, string version, string description)
    {
        services.AddSingleton<IApplicationInfo>(_ => new ApplicationInfo(name, version, description));
        return services;
    }

    public static IBenzeneServiceContainer AddBenzene(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<ITransportsInfo, TransportsInfo>();

        services.TryAddScoped<CurrentTransportInfo>();
        services.TryAddScoped<ICurrentTransport>(x => x.GetService<CurrentTransportInfo>());
        services.TryAddScoped<ISetCurrentTransport>(x => x.GetService<CurrentTransportInfo>());

        services.TryAddSingleton<IApplicationInfo, BlankApplicationInfo>();
        services.TryAddSingleton<IValidationSchemaBuilder, BlankValidationSchemaBuilder>();
        services.TryAddSingleton<IVersionSelector, VersionSelector>();
        services.TryAddSingleton<ISerializer, JsonSerializer>();
        services.TryAddSingleton<JsonSerializer>();
        services.AddDefaultBenzeneLogging();
        services.AddBenzeneMiddleware();
        return services;
    }
    
    public static IBenzeneServiceContainer AddDefaultBenzeneLogging(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IBenzeneLogger, BenzeneLogger>();
        services.TryAddScoped<IBenzeneLogContext, NullBenzeneLogContext>();
        services.AddServiceResolver();
        return services;
    }

}
