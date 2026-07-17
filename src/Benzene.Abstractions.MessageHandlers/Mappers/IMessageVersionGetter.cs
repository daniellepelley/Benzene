namespace Benzene.Abstractions.MessageHandlers.Mappers;

/// <summary>
/// Extracts the payload schema version from a transport-specific message context, so a router can
/// combine it with the topic (<see cref="IMessageTopicGetter{TContext}"/>) into the
/// <see cref="Benzene.Abstractions.Messages.ITopic"/> used for handler-version dispatch, and so
/// version-aware request/response mapping can pick the right payload schema to cast from/to.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the version is extracted from.</typeparam>
public interface IMessageVersionGetter<TContext>
{
    /// <summary>Extracts the payload schema version from the given context.</summary>
    /// <param name="context">The transport-specific context for the incoming message.</param>
    /// <returns>
    /// The version signalled by the message, or <c>null</c>/empty if none was signalled - not an
    /// error, this means "the topic's default version" (docs/specification/versioning.md §2.2).
    /// </returns>
    string? GetVersion(TContext context);
}
