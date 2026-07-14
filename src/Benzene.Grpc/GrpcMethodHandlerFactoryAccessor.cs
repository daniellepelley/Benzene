namespace Benzene.Grpc;

public class GrpcMethodHandlerFactoryAccessor : IGrpcMethodHandlerFactoryAccessor
{
    public IGrpcMethodHandlerFactory? Factory { get; set; }
}
