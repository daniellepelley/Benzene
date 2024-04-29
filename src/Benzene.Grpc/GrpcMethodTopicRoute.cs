namespace Benzene.Grpc;

public class GrpcMethodTopicRoute
{
    public GrpcMethodTopicRoute(string topic, IDictionary<string, object> parameters)
    {
        Topic = topic;
        Parameters = parameters;
    }

    public string Topic { get; }
    public IDictionary<string, object> Parameters { get; }
}
