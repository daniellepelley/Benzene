namespace Benzene.Http.BenzeneMessage;

/// <summary>
/// Describes the BenzeneMessage-over-HTTP endpoint a service exposes. Registered in DI by
/// <c>UseBenzeneMessage</c> so other components — notably the <c>benzene</c> spec builder, which
/// advertises the endpoint as the top-level <c>messageEndpoint</c> field — can discover it.
/// </summary>
public interface IBenzeneMessageHttpEndpointInfo
{
    /// <summary>Gets the path the endpoint listens on (for example <c>/benzene-message</c>).</summary>
    string Path { get; }
}

/// <summary>
/// Default <see cref="IBenzeneMessageHttpEndpointInfo"/> implementation.
/// </summary>
public class BenzeneMessageHttpEndpointInfo : IBenzeneMessageHttpEndpointInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageHttpEndpointInfo"/> class.
    /// </summary>
    /// <param name="path">The path the endpoint listens on.</param>
    public BenzeneMessageHttpEndpointInfo(string path)
    {
        Path = path;
    }

    /// <inheritdoc />
    public string Path { get; }
}
