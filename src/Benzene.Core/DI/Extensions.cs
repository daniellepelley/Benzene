using System;
using System.Linq;
using System.Reflection;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Serialization;
using Benzene.Abstractions.Validation;
using Benzene.Core.DirectMessage;
using Benzene.Core.Helper;
using Benzene.Core.Info;
using Benzene.Core.Logging;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.Core.Validation;
using CacheMessageHandlersFinder = Benzene.Core.MessageHandling.CacheMessageHandlersFinder;

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
        var finder = new CacheMessageHandlersFinder(new ReflectionMessageHandlersFinder(types));
        foreach (var handler in finder.FindDefinitions())
        {
            services.AddScoped(handler.HandlerType);
        }

        services.AddContextItems();
        
        services.AddSingleton<IMessageHandlersFinder>(_ => finder);
        services.TryAddScoped<IMessageHandlersLookUp, MessageHandlersLookUp>();
        services.TryAddScoped<IHandlerPipelineBuilder, HandlerPipelineBuilder>();
        services.TryAddScoped<IMessageHandlerWrapper, PipelineMessageHandlerWrapper>();
        services.TryAddScoped<IMessageHandlerFactory, MessageHandlerFactory>();
        services.TryAddScoped<IValidationSchemaBuilder, BlankValidationSchemaBuilder>();
        
        return services;
    }

    public static IBenzeneServiceContainer AddDirectMessage(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IRequestMapper<DirectMessageContext>, MultiSerializerOptionsRequestMapper<DirectMessageContext, JsonSerializer>>();
        services.TryAddScoped<IMessageMapper<DirectMessageContext>, DirectMessageMapper>();
        services.TryAddScoped<IMessageBodyMapper<DirectMessageContext>, DirectMessageMapper>();
        services.TryAddScoped<IMessageTopicMapper<DirectMessageContext>, DirectMessageMapper>();
        services.TryAddScoped<IMessageHeadersMapper<DirectMessageContext>, DirectMessageMapper>();
        services.TryAddScoped<IBenzeneResponseAdapter<DirectMessageContext>, DirectMessageResponseAdapter>();
        
        services.TryAddScoped<IResponseHandler<DirectMessageContext>, ResponseBodyHandler<DirectMessageContext>>();
        services.AddScoped<IResponseHandler<DirectMessageContext>, DefaultResponseStatusHandler<DirectMessageContext>>();
        services.TryAddScoped<IResponsePayloadMapper<DirectMessageContext>, DefaultResponsePayloadMapper<DirectMessageContext>>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("direct"));
        
        return services;
    }


    public static IBenzeneServiceContainer AddContextItems(this IBenzeneServiceContainer services) 
    {
        services.TryAddScoped(typeof(IMessageMapper<>), typeof(MessageMapper<>));
        services.TryAddScoped(typeof(MessageRouter<>));
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
        services.TryAddSingleton<IMiddlewareFactory, DefaultMiddlewareFactory>();
        services.TryAddSingleton<IVersionSelector, VersionSelector>();
        services.TryAddSingleton<IBenzeneLogger, BenzeneLogger>();
        services.TryAddScoped<IBenzeneLogContext, NullBenzeneLogContext>();
        services.TryAddSingleton<ISerializer, JsonSerializer>();
        services.TryAddSingleton<JsonSerializer>();

        services.AddServiceResolver();
        return services;
    }

    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped<TImplementation>();
    }

    public static IBenzeneServiceContainer TryAddScoped<TService, TImplementation>(
        this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddScoped<TService, TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddScoped(
        this IBenzeneServiceContainer source, Type serviceType, Type implementationType)
    {
        return source.IsTypeRegistered(serviceType)
            ? source
            : source.AddScoped(serviceType, implementationType);
    }

    public static IBenzeneServiceContainer TryAddScoped(this IBenzeneServiceContainer source, Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddScoped(type);
    }
    
    public static IBenzeneServiceContainer TryAddScoped<TImplementation>(this IBenzeneServiceContainer source, Func<IServiceResolver, TImplementation> func)
        where TImplementation: class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddScoped(func);
    }
    
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton<TImplementation>();
    }
    
    public static IBenzeneServiceContainer TryAddSingleton<TService, TImplementation>(this IBenzeneServiceContainer source)
        where TService : class
        where TImplementation : class, TService
    {
        return source.IsTypeRegistered<TService>()
            ? source
            : source.AddSingleton<TService, TImplementation>();
    }
    public static IBenzeneServiceContainer TryAddSingleton(this IBenzeneServiceContainer source,Type type)
    {
        return source.IsTypeRegistered(type)
            ? source
            : source.AddScoped(type);
    }
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,TImplementation implementation)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(implementation);
    }
    public static IBenzeneServiceContainer TryAddSingleton<TImplementation>(this IBenzeneServiceContainer source,Func<IServiceResolver, TImplementation> func)
        where TImplementation : class
    {
        return source.IsTypeRegistered<TImplementation>()
            ? source
            : source.AddSingleton(func);
    }
}
