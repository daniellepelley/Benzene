// using System;
// using System.Collections.Generic;
// using Benzene.Abstractions.Hosting;
// using Benzene.Abstractions.Middleware;
// using Benzene.Aws.Lambda.Core;
// using Benzene.Aws.Lambda.Core.AwsEventStream;
// using Benzene.Microsoft.Dependencies;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Benzene.Examples.Aws.Tests.Helpers;
//
// public class TestLambdaStartUp<TStartUp> where TStartUp : IStartUp<IServiceCollection, IConfiguration, IMiddlewarePipelineBuilder<AwsEventStreamContext>>, IAwsLambdaEntryPoint
// {
//     private readonly List<Action<IServiceCollection>> _actions = new();
//     private Dictionary<string, string> _dictionary;
//
//     public TestLambdaStartUp<TStartUp> WithConfiguration(Dictionary<string, string> dictionary)
//     {
//         _dictionary = dictionary;
//         return this;
//     }
//
//     public TestLambdaStartUp<TStartUp> WithServices(Action<IServiceCollection> action)
//     {
//         _actions.Add(action);
//         return this;
//     }
//
//     public IAwsLambdaEntryPoint Build()
//     {
//         var startup = Activator.CreateInstance<TStartUp>();
//
//         var configuration = startup.GetConfiguration();
//         var configurationBuilder = new ConfigurationBuilder()
//             .AddConfiguration(configuration)
//             .AddInMemoryCollection(_dictionary);
//
//         var services = new ServiceCollection();
//         var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));
//         
//         startup.ConfigureServices(services, configurationBuilder.Build());
//         foreach (var action in _actions)
//         {
//             action(services);
//         }
//         
//         startup.Configure(app, configuration);
//
//         var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
//         return new AwsLambdaEntryPoint(app.Build(), serviceResolverFactory);
//     }
// }