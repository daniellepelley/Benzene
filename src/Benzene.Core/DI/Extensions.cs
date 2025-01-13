using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Serialization;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Info;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Response;

namespace Benzene.Core.DI;

public static class Extensions
{
    public static IBenzeneServiceContainer AddBenzeneMessage(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IMessageGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageBodyGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageTopicGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageHeadersGetter<BenzeneMessageContext>, BenzeneMessageGetter>();
        services.TryAddScoped<IMessageHandlerResultSetter<BenzeneMessageContext>, BenzeneMessageMessageHandlerResultSetter>();
        services.TryAddScoped<IBenzeneResponseAdapter<BenzeneMessageContext>, BenzeneMessageResponseAdapter>();
    
        services.TryAddScoped<IResponseHandler<BenzeneMessageContext>, ResponseBodyHandler<BenzeneMessageContext>>();
        services.AddScoped<IResponseHandler<BenzeneMessageContext>, DefaultResponseStatusHandler<BenzeneMessageContext>>();
        services.TryAddScoped<IResponsePayloadMapper<BenzeneMessageContext>, DefaultResponsePayloadMapper<BenzeneMessageContext>>();
    
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("direct"));
    
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
        services.TryAddSingleton<IDefaultStatuses, DefaultStatuses>();
        services.TryAddSingleton<ITransportsInfo, TransportsInfo>();

        services.TryAddScoped<CurrentTransportInfo>();
        services.TryAddScoped<ICurrentTransport>(x => x.GetService<CurrentTransportInfo>());
        services.TryAddScoped<ISetCurrentTransport>(x => x.GetService<CurrentTransportInfo>());

        services.TryAddSingleton<IApplicationInfo, BlankApplicationInfo>();
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
