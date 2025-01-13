using System.Text.Json;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

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

    private async Task<TResponse?> CallAsync<TRequest, TResponse>(string topic, TRequest request)
    {
        var context = new GrpcContext<TRequest, TResponse>(topic, request);

        await _middlewarePipeline.HandleAsync(context,
            _serviceResolverFactory.CreateScope());
        return context.Response;
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
        var result = await CallAsync<TRequest, TResponse>(_grpcMethodDefinition.Topic, request);

        if (result is TResponse response)
        {
            return response;
        }

        var responseJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
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
        }, _serviceResolverFactory);

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