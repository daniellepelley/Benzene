using System.Collections.Generic;

namespace Benzene.Mesh.Dispatch;

/// <summary>The message to dispatch to a target service (the standard Benzene message envelope).</summary>
public class MeshDispatchEnvelope
{
    /// <summary>Initializes a new instance of the <see cref="MeshDispatchEnvelope"/> class.</summary>
    public MeshDispatchEnvelope(string topic, IDictionary<string, string> headers, string body)
    {
        Topic = topic;
        Headers = headers;
        Body = body;
    }

    /// <summary>The topic to invoke on the target service.</summary>
    public string Topic { get; }

    /// <summary>Message headers to carry.</summary>
    public IDictionary<string, string> Headers { get; }

    /// <summary>The serialized message body.</summary>
    public string Body { get; }
}

/// <summary>The response a target service returned to a dispatched message.</summary>
public class MeshDispatchResult
{
    /// <summary>Initializes a new instance of the <see cref="MeshDispatchResult"/> class.</summary>
    public MeshDispatchResult(string statusCode, string? body, IDictionary<string, string>? headers = null)
    {
        StatusCode = statusCode;
        Body = body;
        Headers = headers ?? new Dictionary<string, string>();
    }

    /// <summary>The status the service returned (a Benzene result status, or an HTTP status for HTTP targets).</summary>
    public string StatusCode { get; }

    /// <summary>Any response headers the service returned.</summary>
    public IDictionary<string, string> Headers { get; }

    /// <summary>The response body the service returned.</summary>
    public string? Body { get; }
}
