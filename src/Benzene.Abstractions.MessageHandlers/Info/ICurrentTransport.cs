namespace Benzene.Abstractions.MessageHandlers.Info;

/// <summary>
/// Read side of the current-transport pairing with <see cref="ISetCurrentTransport"/>: reports which
/// registered transport (see <see cref="ITransportsInfo"/>) is handling the message currently being
/// processed, so shared handler/middleware code can behave differently per transport when needed.
/// </summary>
public interface ICurrentTransport
{
    /// <summary>The name of the transport currently handling the message.</summary>
    string Name { get; }
}