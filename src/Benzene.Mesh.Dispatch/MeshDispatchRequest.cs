using System.Collections.Generic;

namespace Benzene.Mesh.Dispatch;

/// <summary>A request to dispatch a test message to one registered service (the <c>mesh:dispatch</c> body).</summary>
public class MeshDispatchRequest
{
    /// <summary>The target service's name (its key in the mesh registry).</summary>
    public string? Service { get; set; }

    /// <summary>The topic to invoke on the target service.</summary>
    public string? Topic { get; set; }

    /// <summary>Message headers to carry (optional).</summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>The serialized message body (optional).</summary>
    public string? Body { get; set; }
}
