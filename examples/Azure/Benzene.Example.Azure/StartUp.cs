// using System;
// using Benzene.Azure.AspNet;
// using Benzene.Azure.Core;
// using Benzene.Core.MessageHandling;
// using Benzene.Core.Middleware;
// using Benzene.Example.Azure;
// using Benzene.FluentValidation;
// using Benzene.Http.Cors;
// using Benzene.Schema.OpenApi;
// using Microsoft.Azure.WebJobs.Hosting;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
//
// [assembly: WebJobsStartup(typeof(StartUp))]
// namespace Benzene.Example.Azure;
//
// public class StartUp: AzureFunctionStartUp
// {
//     public override IConfiguration GetConfiguration()
//     {
//         return DependenciesBuilder.GetConfiguration();
//     }
//
//     public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
//     {
//         DependenciesBuilder.Register(services, configuration);
//     }
//
//     public override void Configure(AzureFunctionAppBuilder app, IConfiguration configuration)
//     {
//         app.UseHttp(http => http
//             .OnRequest("strip-api", x => x.HttpRequest.Path = x.HttpRequest.Path.Value.Replace("/api", ""))
//             .UseProcessResponse()
//             .UseCors(new CorsSettings
//             {
//                 AllowedDomains = new []{ "https://editor-next.swagger.io" },
//                 AllowedHeaders = Array.Empty<string>() 
//             })
//             .UseSpec()
//             .UseMessageHandlers(router => router.UseFluentValidation()));
//     }
// }