using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public static class Extensions
{
    public static IServiceCollection UsingBenzene(this IServiceCollection services)
    {
        CreateMicrosoftBenzeneServiceContainer(services);
        return services;
    }
    
    public static IServiceCollection UsingBenzene(this IServiceCollection services, Action<IBenzeneServiceContainer> action)
    {
        var microsoftBenzeneServiceContainer = CreateMicrosoftBenzeneServiceContainer(services);
        action(microsoftBenzeneServiceContainer);
        return services;
    }

    private static MicrosoftBenzeneServiceContainer CreateMicrosoftBenzeneServiceContainer(IServiceCollection services)
    {
        var microsoftBenzeneServiceContainer = new MicrosoftBenzeneServiceContainer(services);
        microsoftBenzeneServiceContainer.AddScoped<IBenzeneServiceContainer>(_ => microsoftBenzeneServiceContainer);
        microsoftBenzeneServiceContainer.AddServiceResolver();
        return microsoftBenzeneServiceContainer;
    }
}