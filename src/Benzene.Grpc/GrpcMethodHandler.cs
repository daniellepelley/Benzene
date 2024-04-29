using System.Text.Json;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.BenzeneMessage;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using static System.Net.Mime.MediaTypeNames;

namespace Benzene.Grpc;

public class GrpcMethodHandler : IGrpcMethodHandler
{
    private IGrpcMethodDefinition _grpcMethodDefinition;
    private IServiceResolverFactory _serviceResolverFactory;
    private ServiceDescriptor _serviceDescriptor;
    private IMiddlewarePipeline<GrpcContext> _middlewarePipeline;

    public GrpcMethodHandler(IGrpcMethodDefinition grpcMethodDefinition, IServiceResolverFactory serviceResolverFactory, IMiddlewarePipeline<GrpcContext> middlewarePipeline, ServiceDescriptor serviceDescriptor)
    {
        _middlewarePipeline = middlewarePipeline;
        _serviceDescriptor = serviceDescriptor;
        _serviceResolverFactory = serviceResolverFactory;
        _grpcMethodDefinition = grpcMethodDefinition;
    }

    private async Task<object> CallAsync<TRequest>(string topic, TRequest request)
    {
        var context = new GrpcContext<TRequest>(topic, request);

        await _middlewarePipeline.HandleAsync(context,
            _serviceResolverFactory.CreateScope());
        return context.MessageResult.Payload;
    }

    private T Parse<T>(string json, string methodName) where T : class
    {
        var methodDescriptor = _serviceDescriptor.FindMethodByName(methodName).OutputType;
        return JsonParser.Default.Parse(json, methodDescriptor) as T;
    }

    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var result = await CallAsync(_grpcMethodDefinition.Topic, request);

        if (result is TRequest)
        {
            return result as TResponse;
        }

        var responseJson = System.Text.Json.JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true
        });
        return Parse<TResponse>(responseJson, _grpcMethodDefinition.Method.Split("/").Last());
    }
}
public class GrpcMethodHandler2 : IGrpcMethodHandler
{
    private IGrpcMethodDefinition _grpcMethodDefinition;
    private IServiceResolverFactory _serviceResolverFactory;
    private ServiceDescriptor _serviceDescriptor;
    private BenzeneMessageApplication _application;

    public GrpcMethodHandler2(IGrpcMethodDefinition grpcMethodDefinition, IServiceResolverFactory serviceResolverFactory, IMiddlewarePipeline<BenzeneMessageContext> middlewarePipeline, ServiceDescriptor serviceDescriptor)
    {
        _application = new BenzeneMessageApplication(middlewarePipeline);
        _serviceDescriptor = serviceDescriptor;
        _serviceResolverFactory = serviceResolverFactory;
        _grpcMethodDefinition = grpcMethodDefinition;
    }

    private async Task<string> CallAsync<TRequest>(string topic, TRequest request)
    {
        var result = await _application.HandleAsync(new BenzeneMessageRequest
        {
            Topic = topic,
            Body = JsonSerializer.Serialize(request)
        }, _serviceResolverFactory.CreateScope());

        return result.Body;
    }

    private T Parse<T>(string json, string methodName) where T : class
    {
        var methodDescriptor = _serviceDescriptor.FindMethodByName(methodName).OutputType;
        return JsonParser.Default.Parse(json, methodDescriptor) as T;
    }

    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var responseJson = await CallAsync(_grpcMethodDefinition.Topic, request);

        return Parse<TResponse>(responseJson, _grpcMethodDefinition.Method.Split("/").Last());
    }
}