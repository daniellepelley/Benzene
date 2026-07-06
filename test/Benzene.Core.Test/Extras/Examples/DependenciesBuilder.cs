using System.IO;
using System.Reflection;
// using Amazon.Extensions.NETCore.Setup;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Test.Elements.Examples
{
    public static class DependenciesBuilder
    {
        public static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();
        }

        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            services.UsingBenzene(x => x
                .AddHttpMessageHandlers()
                .AddBenzene()
                .AddScoped<IExampleService, StubbedExampleService>()
            );
        }
    }
}
