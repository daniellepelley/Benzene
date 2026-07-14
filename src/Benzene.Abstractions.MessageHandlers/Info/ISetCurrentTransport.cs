namespace Benzene.Abstractions.MessageHandlers.Info;

/// <summary>
/// Write side of the current-transport pairing with <see cref="ICurrentTransport"/>. A transport
/// adapter calls <see cref="SetTransport"/> once it starts handling a message, so
/// <see cref="ICurrentTransport"/> can later report which transport is active for the invocation.
/// </summary>
public interface ISetCurrentTransport
{
    /// <summary>Records the name of the transport currently handling a message.</summary>
    /// <param name="transport">The transport name (e.g. matching an <see cref="ITransportInfo"/> entry).</param>
    void SetTransport(string transport);
}