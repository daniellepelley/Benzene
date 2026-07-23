namespace Benzene.Clients
{
    /// <summary>
    /// The response envelope returned by a Benzene service invoked through a message client. Deserializes
    /// from the standard Benzene message envelope (<c>{ "statusCode": ..., "headers": { ... }, "body": "..." }</c>)
    /// written by the serving side's <c>BenzeneMessageResponse</c> — see
    /// <c>docs/specification/wire-contracts.md</c>. <see cref="StatusCode"/> carries a Benzene result status
    /// (e.g. <c>"ok"</c>, <c>"not-found"</c>); numeric HTTP status codes from older or HTTP-shaped services
    /// are also tolerated by <c>AsBenzeneResult</c>.
    /// </summary>
    public class BenzeneMessageClientResponse
    {
        public BenzeneMessageClientResponse(string statusCode, string body, IDictionary<string, string>? headers = null)
        {
            StatusCode = statusCode;
            Body = body;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string StatusCode { get; }
        public IDictionary<string, string> Headers { get; }
        public string Body { get; }
    }
}
