using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Grpc.Serialization;

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
        return context.RequestAsObject is TRequest direct
            ? direct
            : _adapter.ConvertRequest<TRequest>(context.RequestAsObject);
    }
}
