using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.Response;

namespace Benzene.Xml;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddXml(this IBenzeneServiceContainer services)
    {
        services.AddSingleton(typeof(ISerializerOption<>), typeof(XmlSerializerOption<>));
        services.AddSingleton(typeof(IResponseHandler<>), typeof(XmlResponseHandler<>));
        services.AddSingleton(typeof(XmlSerializationResponseHandler<>));
        services.AddSingleton<XmlSerializer>();
        return services;
    }

    public static IBenzeneServiceContainer AddXml<TContext>(this IBenzeneServiceContainer services) where TContext : class, IHasMessageResult
    {
        services.AddSingleton(typeof(ISerializerOption<TContext>), typeof(XmlSerializerOption<TContext>));
        services
            .AddScoped<IResponseHandler<TContext>,
                ResponseHandler<XmlSerializationResponseHandler<TContext>, TContext>>();
        services.AddScoped<XmlSerializationResponseHandler<TContext>>();
        services.AddSingleton<XmlSerializer>();
        return services;
    }
    
    public static IMiddlewarePipelineBuilder<TContext> UseXml<TContext>(this IMiddlewarePipelineBuilder<TContext> source)
       where TContext : class, IHasMessageResult

    {
        source.Register(x => x.AddXml<TContext>());
        return source;
    }
}
