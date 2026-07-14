using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Core.MessageHandlers
{
    /// <summary>
    /// Well-known constants shared across the message handler pipeline (routing, headers, content
    /// types).
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Gets a sentinel <see cref="ITopic"/> (id <c>"&lt;missing&gt;"</c>) used wherever a message's
        /// topic could not be determined, e.g. when no handler could be resolved for an inbound message.
        /// </summary>
        public static ITopic Missing => new Topic("<missing>");

        /// <summary>
        /// The lower-case header name used for the content-type of a message body ("content-type").
        /// </summary>
        public const string ContentTypeHeader = "content-type";

        /// <summary>
        /// The MIME type value ("application/json") used when a response body is JSON-encoded.
        /// </summary>
        public const string JsonContentType = "application/json";
    }
}
