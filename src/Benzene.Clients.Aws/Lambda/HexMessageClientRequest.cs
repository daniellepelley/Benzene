using System.Collections.Generic;

namespace Benzene.Clients.Aws.Lambda
{
    public class BenzeneMessageClientRequest
    {
        public BenzeneMessageClientRequest(string topic, IDictionary<string, string> headers, string message)
        {
            Topic = topic;
            Headers = headers;
            Message = message;
        }

        public string Topic { get; }
        public IDictionary<string, string> Headers { get; }
        public string Message { get; }
    }
}
