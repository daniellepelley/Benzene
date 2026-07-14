using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Integration.Test.Helpers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureServiceCollection(this IServiceCollection services)
    {
        return services.UsingBenzene(x =>
        {
            x.AddBenzene();
            x.AddMessageHandlers(typeof(ExampleMessageHandler).Assembly);
        });
    }
}
