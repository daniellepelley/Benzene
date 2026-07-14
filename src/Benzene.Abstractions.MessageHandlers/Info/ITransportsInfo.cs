namespace Benzene.Abstractions.MessageHandlers.Info;

/// <summary>Exposes the names of every transport registered with the application (e.g. for diagnostics or startup logging).</summary>
public interface ITransportsInfo
{
    /// <summary>The names of all registered transports.</summary>
    string[] Transports { get; }
}
