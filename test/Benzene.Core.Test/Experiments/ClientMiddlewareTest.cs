// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Benzene.Abstractions.DI;
// using Benzene.Abstractions.Middleware;
// using Benzene.Clients;
// using Benzene.Core.BenzeneMessage;
// using Benzene.Core.Middleware;
// using Benzene.Microsoft.Dependencies;
// using Benzene.Results;
// using Microsoft.Extensions.DependencyInjection;
// using Xunit;
//
// namespace Benzene.Test.Experiments;
//
// public class ClientMiddlewareTest
// {
//     [Fact]
//     public async Task Send()
//     {
//         var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
//         // var appBuilder = new MiddlewarePipelineBuilder<IMessageContext<>>(services);
//
//         // var client = new MiddlewareBenzeneMessageClient2(appBuilder.Build(), );
//
//
//
//     }
// }
//
// public class MiddlewareBenzeneMessageClient2 : IBenzeneMessageClient
// {
//     private readonly IMiddlewarePipeline<BenzeneMessageContext> _middlewarePipeline;
//     private readonly IServiceResolver _serviceResolver;
//
//     public MiddlewareBenzeneMessageClient2(
//         IMiddlewarePipeline<BenzeneMessageContext> middlewarePipeline,
//         IServiceResolver serviceResolver)
//     {
//         _serviceResolver = serviceResolver;
//         _middlewarePipeline = middlewarePipeline;
//     }
//
//     public void Dispose()
//     {
//     }
//
//     public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
//     {
//         await _middlewarePipeline.HandleAsync(context, _serviceResolver);
//         return BenzeneResult.Set<TResponse>(context.BenzeneMessageResponse.StatusCode, context.BenzeneMessageResponse);
//     }
// }
//
