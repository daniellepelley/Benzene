using System;
using Benzene.Abstractions.DI;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Benzene.Test.Examples;

public static class ServiceResolverMother
{
    public static IServiceResolver CreateServiceResolver()
    {
        var serviceResolver = new MicrosoftServiceResolverFactory(CreateServiceCollection()).CreateScope();
        return serviceResolver;
    }

    public static IServiceCollection CreateServiceCollection()
    {
        return CreateServiceCollection(x => { });
    }

    public static IServiceCollection CreateServiceCollection(Action<IBenzeneServiceContainer> register)
    {
        return ConfigureServiceCollection(new ServiceCollection(), register);
    }

    public static IServiceCollection ConfigureServiceCollection(this IServiceCollection services)
    {
        return ConfigureServiceCollection(services, x => { });
    }

    public static IServiceCollection ConfigureServiceCollection(IServiceCollection services, Action<IBenzeneServiceContainer> register)
    {
        var assembly = typeof(ExampleRequestPayload).Assembly;
        services.UsingBenzene(x =>
        {
            x.AddBenzene();
            // x.AddCorrelationId();
            x.AddMessageHandlers(assembly);
            register(x);
        });

        services.AddScoped<BenzeneMessageMapper>();
        services.AddSingleton(Mock.Of<IExampleService>());
        return services;
    }


}
