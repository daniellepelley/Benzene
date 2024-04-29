namespace Benzene.Grpc;

public interface IGrpcMethodDefinition
{
    string Method { get; }
    string Topic { get; }
}