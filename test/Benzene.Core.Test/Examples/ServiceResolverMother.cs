using System;
using Autofac;
using Benzene.Abstractions.DI;
using Benzene.Autofac;
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

    public static ContainerBuilder ConfigureServiceCollection(this ContainerBuilder services)
    {
        return ConfigureServiceCollection(services, x => { });
    }

    public static IServiceCollection ConfigureServiceCollection(IServiceCollection services, Action<IBenzeneServiceContainer> register)
    {
        return services.UsingBenzene(x =>
        {
            ConfigureServiceCollection(x, register);
        });
    }

    public static ContainerBuilder ConfigureServiceCollection(ContainerBuilder services, Action<IBenzeneServiceContainer> register)
    {
        return services.UsingBenzene(x =>
        {
            ConfigureServiceCollection(x, register);
        });
    }

    public static IBenzeneServiceContainer ConfigureServiceCollection(IBenzeneServiceContainer container, Action<IBenzeneServiceContainer> register)
    {
        var assembly = typeof(ExampleRequestPayload).Assembly;
        container.AddBenzene();
        container.AddMessageHandlers(assembly);
        container.AddScoped<BenzeneMessageMapper>();
        container.AddSingleton(Mock.Of<IExampleService>());
        register(container);
        return container;
    }
}
