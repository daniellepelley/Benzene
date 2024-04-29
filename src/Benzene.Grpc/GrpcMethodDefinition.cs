namespace Benzene.Grpc;

public class GrpcMethodDefinition : IGrpcMethodDefinition
{
    public GrpcMethodDefinition(string method, string topic)
    {
        Method = method;
        Topic = topic;
    }

    public string Method { get; }
    public string Topic { get; }
}
