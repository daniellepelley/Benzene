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
                // .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }

        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            var region = configuration.GetValue<string>("AWS_DEFAULT_REGION");
            var serviceUrl = configuration.GetValue<string>("AWS_SERVICE_URL");
            // var awsOptions = new AWSOptions
            // {
            //     Region = string.IsNullOrWhiteSpace(region)
            //         ? RegionEndpoint.EUWest2
            //         : RegionEndpoint.GetBySystemName(region),
            // };

//             if (!string.IsNullOrEmpty(serviceUrl))
//             {
//                 awsOptions.DefaultClientConfig.ServiceURL = serviceUrl;
//             }
// #if DEBUG
//             awsOptions.Profile = "developer@darwindevelopment";
// #endif

            services.AddSingleton(configuration);
            // services.AddTransient(_ => configuration.GetAWSOptions());
            // services.AddSingleton(awsOptions.CreateServiceClient<IAmazonSQS>());

            var assembly = Assembly.GetExecutingAssembly();
            services.UsingBenzene(x => x
                // .AddMessageHandlers(assembly)
                .AddHttpMessageHandlers()
                .AddBenzene()
                // .AddBenzeneMessage()
                // .AddApiGateway()
                // .AddMicrosoftLogger()
                // .AddServiceResolver()
                // .AddCorrelationId()
                // .AddPermissions()
                // .AddEventBus()
                // .AddDiagnostics()
                // .AddScoped<IHttpHeaderMappings, ElementsHttpHeaderMappings>()
                // .TryAddScoped<IRequestMapper<BenzeneMessageContext>, MultiSerializerOptionsRequestMapper<BenzeneMessageContext, PatchJsonSerializer>>()
                // .AddScoped<IMiddlewareWrapper, TimerMiddlewareWrapper>()
                .AddScoped<IExampleService, StubbedExampleService>()
                // .AddScoped(_ => (JsonSerializer)new PatchJsonSerializer())
            );
        }
    }
}
