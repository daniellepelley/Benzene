using Benzene.Grpc.Serialization;
using Grpc.Core;

namespace Benzene.Grpc.Client;

/// <summary>
/// Converts <see cref="GrpcSendMessageContext.Message"/> (POCO or protobuf) into the route's protobuf
/// <typeparamref name="TRequest"/> via <see cref="IGrpcMessageAdapter.ConvertResponse{TResponse}"/> - the
/// same "produce a protobuf from a payload" direction the server uses for its outgoing responses - then
/// invokes the call and stores the raw protobuf response back on the context untouched; converting that
/// back into the caller's own response type is <see cref="GrpcBenzeneMessageClient"/>'s job.
/// </summary>
public class GrpcClientRoute<TRequest, TResponse> : IGrpcClientRoute
    where TRequest : class
    where TResponse : class
{
    private readonly Method<TRequest, TResponse> _method;

    public GrpcClientRoute(Method<TRequest, TResponse> method)
    {
        _method = method;
    }

    public async Task InvokeAsync(CallInvoker invoker, IGrpcMessageAdapter adapter, GrpcSendMessageContext context)
    {
        var request = adapter.ConvertResponse<TRequest>(context.Message);
        var options = new CallOptions(context.Headers, context.Deadline, context.CancellationToken);

        using var call = invoker.AsyncUnaryCall(_method, null, options, request);
        context.Response = await call.ResponseAsync;
        context.Status = call.GetStatus();
        context.ResponseTrailers = call.GetTrailers();
    }
}
