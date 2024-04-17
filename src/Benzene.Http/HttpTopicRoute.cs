namespace Benzene.Http;

public class HttpTopicRoute
{
    public HttpTopicRoute(string topic, IDictionary<string, object> parameters)
    {
        Topic = topic;
        Parameters = parameters;
    }

    public string Topic { get; }
    public IDictionary<string, object> Parameters { get; }
}
