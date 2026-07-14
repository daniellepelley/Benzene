using System.Collections.Generic;

namespace Benzene.Clients.Aws.Lambda
{
    /// <summary>
    /// The envelope sent to a target Lambda function, carrying the message topic, headers, and serialized
    /// message body. Serializes to the standard Benzene message envelope
    /// (<c>{ "topic": ..., "headers": { ... }, "body": "..." }</c>) that the receiving side's
    /// <c>BenzeneMessageRequest</c> deserializes — see <c>docs/specification/wire-contracts.md</c>.
    /// </summary>
    public class BenzeneMessageClientRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BenzeneMessageClientRequest"/> class.
        /// </summary>
        /// <param name="topic">The message topic.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="body">The serialized message body.</param>
        public BenzeneMessageClientRequest(string topic, IDictionary<string, string> headers, string body)
        {
            Topic = topic;
            Headers = headers;
            Body = body;
        }

        /// <summary>
        /// Gets the message topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// Gets the message headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the serialized message body.
        /// </summary>
        public string Body { get; }
    }
}
