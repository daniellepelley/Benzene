using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Aws.ApiGateway;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Mappers;
using Benzene.Core.Middleware;
using Benzene.Http;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws;

public static class Extensions
{
    // public static IMiddlewarePipelineBuilder<TContext> UseRequestMapping<TContext>(
    //     this IMiddlewarePipelineBuilder<TContext> app, Action<RequestMapBuilder<TContext>> action)
    // {
    //     var builder = new RequestMapBuilder<TContext>(app.Register);
    //     action(builder);
    //     return app;
    // }

    // public static IMiddlewarePipelineBuilder<ApiGatewayContext> AsHttp(
    //     this IMiddlewarePipelineBuilder<ApiGatewayContext> app,
    //     Action<IMiddlewarePipelineBuilder<IBenzeneHttpContext>> action)
    // {
    //     var middlewarePipelineBuilder = app.Create<IBenzeneHttpContext>();
    //     action(middlewarePipelineBuilder);
    //     var pipeline = middlewarePipelineBuilder.AsPipeline();
    //
    //     return app.Use(resolver => new FuncWrapperMiddleware<ApiGatewayContext>("AsHttp", async (context, next) =>
    //     {
    //         await pipeline.HandleAsync(new ApiGatewayHttpContextWrapper(context), resolver);
    //         await next();
    //     }));
    // }

    // public static IMiddlewarePipelineBuilder<ApiGatewayContext> AsBenzeneMessage(
    //     this IMiddlewarePipelineBuilder<ApiGatewayContext> app,
    //     Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> action)
    // {
    //     var middlewarePipelineBuilder = app.Create<BenzeneMessageContext>();
    //     action(middlewarePipelineBuilder);
    //     var pipeline = middlewarePipelineBuilder.Build();
    //
    //     return app.Use(resolver => new FuncWrapperMiddleware<ApiGatewayContext>("AsBenzeneMessage", async (context, next) =>
    //     {
    //         await pipeline.HandleAsync(
    //             new BenzeneMessageContext(new ApiGatewayBenzeneRequestWrapper(context.ApiGatewayProxyRequest)),
    //             resolver);
    //         await next();
    //     }));
    // }

    // public static IMiddlewarePipelineBuilder<IBenzeneHttpContext> UseRequestMapping(
    //     this IMiddlewarePipelineBuilder<IBenzeneHttpContext> app, Action<RequestMapBuilder<IBenzeneHttpContext>> action)
    // {
    //     var builder = new RequestMapBuilder<IBenzeneHttpContext>(app.Register);
    //     action(builder);
    //     return app;
    // }

    // public static IMiddlewarePipelineBuilder<TContext> UseLogHeaders<TContext>(
    //     this IMiddlewarePipelineBuilder<TContext> app, params string[] headers) where TContext : IHasMessageResult
    // {
    //     return app.Use(resolver => new FuncWrapperMiddleware<TContext>("LogHeaders", async (context, next) =>
    //     {
    //         var logContext = resolver.GetService<ILogContext>();
    //
    //         var dictionary = new Dictionary<string, string>();
    //
    //         foreach (var header in headers)
    //         {
    //             var messageMapper = resolver.GetService<IMessageMapper<TContext>>();
    //             var value = messageMapper.GetHeader(context, header);
    //             if (!string.IsNullOrEmpty(value))
    //             {
    //                 dictionary.Add(header, value);
    //             }
    //         }
    //
    //         using (logContext.Create(dictionary))
    //         {
    //             await next();
    //         }
    //     }));
    // }

    public static IMiddlewarePipelineBuilder<THasResponse> UseLogResult<THasResponse>(
        this IMiddlewarePipelineBuilder<THasResponse> app) where THasResponse : IHasMessageResult
    {
        return app.Use(resolver => new FuncWrapperMiddleware<THasResponse>("LogResult", async (context, next) =>
        {
            var logger = resolver.GetService<IBenzeneLogger>();
            await next();

            var result = context.MessageResult;

            if (result == null)
            {
                return;
            }

            logger.LogInformation("Lambda Response status {response}", result.Status);
        }));
    }

    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHttpToBenzeneMessage(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, IMiddlewarePipelineBuilder<BenzeneMessageContext> middlewarePipelineBuilder)
    {
        var pipeline = middlewarePipelineBuilder.Build();
        return app.Use(resolver => new FuncWrapperMiddleware<ApiGatewayContext>("HttpToBenzene", async (context, next) =>
        {
            if (context.ApiGatewayProxyRequest.HttpMethod == "POST" &&
                context.ApiGatewayProxyRequest.Path == "admin/benzene-message")
            {
                var httpStatusCodeMapper = resolver.GetService<IHttpStatusCodeMapper>();
                var benzeneMessage = JsonConvert.DeserializeObject<BenzeneMessageRequest>(context.ApiGatewayProxyRequest.Body);
    
                var benzeneMessageContext = new BenzeneMessageContext(benzeneMessage);
                await pipeline.HandleAsync(benzeneMessageContext, resolver);
                context.ApiGatewayProxyResponse = new APIGatewayProxyResponse
                {
                    StatusCode = Convert.ToInt32(httpStatusCodeMapper.Map(benzeneMessageContext.MessageResult.Status)),
                    Body = JsonConvert.SerializeObject(benzeneMessageContext.MessageResult.Payload)
                };
            }
            else
            {
                await next();
            }

        }));
        
    }

    public class AdminBenzeneMessageMiddleware : IMiddleware<ApiGatewayContext>
    {
        private readonly IHttpStatusCodeMapper _httpStatusCodeMapper;
        private readonly IMiddlewarePipelineBuilder<BenzeneMessageContext> _middleware;
        private readonly IServiceResolver _serviceResolver;
    
        public AdminBenzeneMessageMiddleware(IServiceResolver serviceResolver, IHttpStatusCodeMapper httpStatusCodeMapper)
        {
            _httpStatusCodeMapper = httpStatusCodeMapper;
            _serviceResolver = serviceResolver;
            // _middleware = new MiddlewarePipelineBuilder<BenzeneMessageContext>()
            //     .UseMessageRouter(x => x.UseFluentValidation());
        }
    
        public string Name { get; }
    
        public async Task HandleAsync(ApiGatewayContext context, Func<Task> next)
        {
            if (context.ApiGatewayProxyRequest.HttpMethod == "POST" &&
                context.ApiGatewayProxyRequest.Path == "/admin/benzene-message")
            {
                var benzeneMessage = JsonConvert.DeserializeObject<BenzeneMessageRequest>(context.ApiGatewayProxyRequest.Body);
    
                var benzeneMessageContext = new BenzeneMessageContext(benzeneMessage);
                // await _middleware.HandleAsync(benzeneMessageContext, _serviceResolver);
                // context.ApiGatewayProxyResponse = new APIGatewayProxyResponse
                // {
                //     StatusCode = Convert.ToInt32(_httpStatusCodeMapper.Map(benzeneMessageContext.MessageResult.Status)),
                //     Body = JsonConvert.SerializeObject(benzeneMessageContext.MessageResult.Payload)
                // };
            }
            else
            {
                await next();
            }
        }
    }
}