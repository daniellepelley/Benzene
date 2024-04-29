namespace Benzene.Grpc;

public interface IGrpcMethodHandlerFactory
{
    IGrpcMethodHandler Create(IGrpcMethodDefinition grpcMethodDefinition);
}