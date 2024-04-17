using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public static class Extensions
{
    public static IServiceCollection UsingBenzene(this IServiceCollection services, Action<IBenzeneServiceContainer> action)
    {
        var microsoftBenzeneServiceContainer = new MicrosoftBenzeneServiceContainer(services);
        services.AddScoped<IBenzeneServiceContainer>(_ => microsoftBenzeneServiceContainer);
        action(microsoftBenzeneServiceContainer);
        return services;
    }
}