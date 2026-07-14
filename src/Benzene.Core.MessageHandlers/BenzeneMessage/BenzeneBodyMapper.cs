using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Default <see cref="IMessageGetter{TContext}"/> for the <c>BenzeneMessage</c> transport-agnostic
/// message format: extracts topic, body, and headers from <see cref="BenzeneMessageContext"/>'s
/// underlying <see cref="IBenzeneMessageRequest"/>.
/// </summary>
/// <remarks>
/// Despite the file name, this class is <c>BenzeneMessageGetter</c>, not <c>BenzeneBodyMapper</c> -
/// it maps more than just the body (topic and headers too). Registered by <c>AddBenzeneMessage</c>
/// against <see cref="IMessageGetter{TContext}"/> and each of its constituent interfaces.
/// </remarks>
public class BenzeneMessageGetter : IMessageGetter<BenzeneMessageContext>
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
    /// Gets the request's topic, with its version read from the <c>"version"</c> header, or the
    /// <see cref="Constants.Missing"/> sentinel topic if the request has no topic set.
    /// </summary>
    /// <param name="context">The context to extract the topic from.</param>
    /// <returns>The request's topic.</returns>
    public ITopic GetTopic(BenzeneMessageContext context)
    {
        if (context?.BenzeneMessageRequest?.Topic == null)
        {
            return new Topic(Messages.Constants.Missing.Id);
        }

        return new Topic(
            context.BenzeneMessageRequest.Topic,
            Utils.GetValue(context.BenzeneMessageRequest.Headers, "version"));
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
}
