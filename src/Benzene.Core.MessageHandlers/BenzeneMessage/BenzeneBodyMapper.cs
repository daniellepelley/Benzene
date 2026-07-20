using System.Text;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Default <see cref="IMessageGetter{TContext}"/> for the <c>BenzeneMessage</c> transport-agnostic
/// message format: extracts topic, body, and headers from <see cref="BenzeneMessageContext"/>'s
/// underlying <see cref="IBenzeneMessageRequest"/>. Also implements
/// <see cref="IMessageBodyBytesGetter{TContext}"/> (UTF-8 encoding the string body), making
/// <c>BenzeneMessage</c> the reference transport for Phase 4's byte-oriented request-mapping path.
/// </summary>
/// <remarks>
/// Despite the file name, this class is <c>BenzeneMessageGetter</c>, not <c>BenzeneBodyMapper</c> -
/// it maps more than just the body (topic and headers too). Registered by <c>AddBenzeneMessage</c>
/// against <see cref="IMessageGetter{TContext}"/> and each of its constituent interfaces.
/// </remarks>
public class BenzeneMessageGetter : IMessageGetter<BenzeneMessageContext>, IMessageBodyBytesGetter<BenzeneMessageContext>
{
    /// <summary>
    /// Gets the request's headers, or an empty dictionary if none are set.
    /// </summary>
    /// <param name="context">The context to extract headers from.</param>
    /// <returns>The request's headers.</returns>
    public IDictionary<string, string> GetHeaders(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Headers ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets the request's topic id (version-less), or the <see cref="Constants.Missing"/> sentinel
    /// topic if the request has no topic set.
    /// </summary>
    /// <param name="context">The context to extract the topic from.</param>
    /// <returns>The request's topic.</returns>
    /// <remarks>
    /// Deliberately returns no version: the payload schema version is resolved by the router's
    /// configurable, priority-ordered <see cref="IMessageVersionGetter{TContext}"/> (default
    /// <c>benzene-version</c> &gt; <c>version</c> &gt; <c>x-version</c>), like every other transport's
    /// topic getter. Baking the raw <c>"version"</c> header in here made <see cref="MessageRouter{TContext}"/>
    /// treat it as a preset override and skip the version getter, defeating both the configured header
    /// order and any app that narrows the list (docs/specification/versioning.md §2.1).
    /// </remarks>
    public ITopic GetTopic(BenzeneMessageContext context)
    {
        if (context?.BenzeneMessageRequest?.Topic == null)
        {
            return new Topic(Messages.Constants.Missing.Id);
        }

        return new Topic(context.BenzeneMessageRequest.Topic);
    }

    /// <summary>
    /// Gets the request's raw body.
    /// </summary>
    /// <param name="context">The context to extract the body from.</param>
    /// <returns>The request's raw body.</returns>
    public string GetBody(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Body;
    }

    /// <summary>
    /// Gets the request's raw body as UTF-8 bytes.
    /// </summary>
    /// <param name="context">The context to extract the body from.</param>
    /// <returns>The request's raw body, UTF-8 encoded, or empty if there is no body.</returns>
    public ReadOnlyMemory<byte> GetBodyBytes(BenzeneMessageContext context)
    {
        var body = context.BenzeneMessageRequest.Body;
        return string.IsNullOrEmpty(body) ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(body);
    }
}
