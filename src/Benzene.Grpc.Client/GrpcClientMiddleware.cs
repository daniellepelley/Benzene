using Benzene.Abstractions.Middleware;
using Benzene.Grpc.Serialization;
using Grpc.Core;

namespace Benzene.Grpc.Client;

public class GrpcClientMiddleware : IMiddleware<GrpcSendMessageContext>
{
    private readonly CallInvoker _callInvoker;
    private readonly IGrpcClientRouteRegistry _routeRegistry;
    private readonly IGrpcMessageAdapter _adapter;

    public GrpcClientMiddleware(CallInvoker callInvoker, IGrpcClientRouteRegistry routeRegistry, IGrpcMessageAdapter adapter)
    {
        _callInvoker = callInvoker;
        _routeRegistry = routeRegistry;
        _adapter = adapter;
    }

    public string Name => nameof(GrpcClientMiddleware);

    public async Task HandleAsync(GrpcSendMessageContext context, Func<Task> next)
    {
        var route = _routeRegistry.Find(context.Topic);
        if (route == null)
        {
            context.Status = new Status(StatusCode.Unimplemented, $"No gRPC route has been registered for topic '{context.Topic}'.");
            return;
        }

        try
        {
            await route.InvokeAsync(_callInvoker, _adapter, context);
        }
        catch (RpcException ex)
        {
            context.Status = ex.Status;
            context.ResponseTrailers = ex.Trailers;
        }
    }
}
