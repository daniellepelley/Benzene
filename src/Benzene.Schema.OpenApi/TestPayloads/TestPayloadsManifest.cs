using System.Collections.Generic;

namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>A manifest of ready-to-fire example payloads for a service's domain topics.</summary>
public class TestPayloadsManifest
{
    /// <summary>One entry per domain topic.</summary>
    public TestPayloadTopic[] Topics { get; set; } = System.Array.Empty<TestPayloadTopic>();
}

/// <summary>The example payloads and reachability for a single topic.</summary>
public class TestPayloadTopic
{
    /// <summary>The topic id.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>The transports the topic is reachable on (wired host transports plus HTTP when mapped).</summary>
    public string[] Transports { get; set; } = System.Array.Empty<string>();

    /// <summary>The topic's HTTP method/path mappings, when any.</summary>
    public TestPayloadHttpMapping[] HttpMappings { get; set; } = System.Array.Empty<TestPayloadHttpMapping>();

    /// <summary>Example payloads keyed by transport (currently the portable <c>benzene-message</c> envelope).</summary>
    public IDictionary<string, object> Payloads { get; set; } = new Dictionary<string, object>();
}

/// <summary>An HTTP method/path a topic is mapped to.</summary>
public class TestPayloadHttpMapping
{
    /// <summary>The HTTP method (GET/POST/…).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>The route template.</summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>The transport-agnostic BenzeneMessage envelope - the shape POSTed to <c>/benzene-message</c>.</summary>
public class BenzeneMessagePayload
{
    /// <summary>The topic to invoke.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Message headers (empty by default).</summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>The JSON-serialized example message body.</summary>
    public string Body { get; set; } = string.Empty;
}
