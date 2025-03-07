﻿using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers.DI;

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
    
    public static IBenzeneServiceContainer AddContextItems(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped(typeof(IMessageGetter<>), typeof(MessageGetter<>));
        services.TryAddScoped(typeof(IResponsePayloadMapper<>), typeof(DefaultResponsePayloadMapper<>));
        services.TryAddScoped(typeof(IResponseHandlerContainer<>), typeof(ResponseHandlerContainer<>));
        services.TryAddScoped(typeof(JsonSerializationResponseHandler<>));

        services.TryAddScoped(typeof(IRequestMapper<>), typeof(JsonDefaultMultiSerializerOptionsRequestMapper<>));
        return services;
    }


    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        params Assembly[] assemblies)
    {
        var types = Utils.GetAllTypes(assemblies).ToArray();
        return services.AddMessageHandlers(types);
    }

    public static IBenzeneServiceContainer AddMessageHandlers(this IBenzeneServiceContainer services,
        Type[] types)
    {
        var cacheMessageHandlersFinder = new CacheMessageHandlersFinder(new ReflectionMessageHandlersFinder(types));
        foreach (var handler in cacheMessageHandlersFinder.FindDefinitions())
        {
            services.AddScoped(handler.HandlerType);
        }
    
        services.TryAddSingleton<MessageHandlersList>();
        services.TryAddSingleton<DependencyMessageHandlersFinder>();
        services.TryAddSingleton<IMessageHandlersList, MessageHandlersList>();
        services.TryAddSingleton<IMessageHandlersFinder>(x =>
            new CompositeMessageHandlersFinder(
                cacheMessageHandlersFinder,
            x.GetService<MessageHandlersList>(),
            x.GetService<DependencyMessageHandlersFinder>()
        ));
    
        services.TryAddScoped<IMessageHandlerDefinitionLookUp, MessageHandlerDefinitionLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped(typeof(MessageRouter<>));

        services.AddContextItems();
        return services;
    }
}
