namespace Benzene.Grpc;

public interface IGrpcRouteFinder
{
    IGrpcMethodDefinition? Find(string method);
}