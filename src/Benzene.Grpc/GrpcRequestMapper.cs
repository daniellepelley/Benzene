using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Streaming;

namespace Benzene.Grpc;

public class GrpcRequestMapper : IRequestMapper<GrpcContext>
{
    private readonly IGrpcMessageAdapter _adapter;

    public GrpcRequestMapper(IGrpcMessageAdapter adapter)
    {
        _adapter = adapter;
    }

    public TRequest? GetBody<TRequest>(GrpcContext context) where TRequest : class
    {
        if (context.RequestAsObject is TRequest direct)
        {
            return direct;
        }

        if (GrpcStreamAdapter.TryConvertStream(context.RequestAsObject, typeof(TRequest), _adapter, isResponseDirection: false, context.CancellationToken) is TRequest convertedStream)
        {
            return convertedStream;
        }

        return _adapter.ConvertRequest<TRequest>(context.RequestAsObject);
    }
}
