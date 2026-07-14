using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Abstractions.MessageHandlers.Mappers;

/// <summary>
/// Convenience aggregate of the three pieces of a transport's incoming message a router needs to
/// extract before it can dispatch: body, headers, and topic. A transport adapter implements this
/// (or its constituent interfaces) once for its context type <typeparamref name="TContext"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type messages are extracted from.</typeparam>
public interface IMessageGetter<TContext>
    : IMessageBodyGetter<TContext>, IMessageHeadersGetter<TContext>, IMessageTopicGetter<TContext>
{}