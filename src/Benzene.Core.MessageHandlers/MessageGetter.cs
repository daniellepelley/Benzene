using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageGetter{TContext}"/> implementation that composes the individually
/// registered <see cref="IMessageTopicGetter{TContext}"/>, <see cref="IMessageBodyGetter{TContext}"/>
/// and <see cref="IMessageHeadersGetter{TContext}"/> for <typeparamref name="TContext"/> into a
/// single facade, so callers that need all three don't have to depend on each mapper individually.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type messages are extracted from.</typeparam>
public class MessageGetter<TContext> : IMessageGetter<TContext>
{
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageGetter{TContext}"/> class.
    /// </summary>
    /// <param name="messageTopicGetter">Extracts the topic from a <typeparamref name="TContext"/>.</param>
    /// <param name="messageBodyGetter">Extracts the body from a <typeparamref name="TContext"/>.</param>
    /// <param name="messageHeadersGetter">Extracts the headers from a <typeparamref name="TContext"/>.</param>
    public MessageGetter(IMessageTopicGetter<TContext> messageTopicGetter, IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter)
    {
        _messageHeadersGetter = messageHeadersGetter;
        _messageTopicGetter = messageTopicGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    /// <summary>
    /// Gets the raw body of the message, via the registered <see cref="IMessageBodyGetter{TContext}"/>.
    /// </summary>
    /// <param name="context">The transport-specific context to extract the body from.</param>
    /// <returns>The raw message body.</returns>
    public string GetBody(TContext context)
    {
        return _messageBodyGetter.GetBody(context);
    }

    /// <summary>
    /// Gets the headers of the message, via the registered <see cref="IMessageHeadersGetter{TContext}"/>.
    /// </summary>
    /// <param name="context">The transport-specific context to extract the headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(TContext context)
    {
        return _messageHeadersGetter.GetHeaders(context);
    }

    /// <summary>
    /// Gets the topic of the message, via the registered <see cref="IMessageTopicGetter{TContext}"/>.
    /// </summary>
    /// <param name="context">The transport-specific context to extract the topic from.</param>
    /// <returns>The message's <see cref="ITopic"/>.</returns>
    public ITopic GetTopic(TContext context)
    {
        return _messageTopicGetter.GetTopic(context);
    }
}
