using System.Collections.Generic;

namespace Benzene.Clients.Aws.Lambda
{
    public class BenzeneMessageClientResponse
    {
        public BenzeneMessageClientResponse(string statusCode, string message, IDictionary<string, string>? headers = null)
        {
            StatusCode = statusCode;
            Message = message;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string StatusCode { get; }
        public IDictionary<string, string> Headers { get; }
        public string Message { get; }
    }
}
