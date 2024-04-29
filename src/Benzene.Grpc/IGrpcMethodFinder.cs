namespace Benzene.Grpc;

public interface IGrpcMethodFinder
{
    IGrpcMethodDefinition[] FindDefinitions();
}