using System;
using System.Collections.Generic;

namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>The inputs an <see cref="ITestPayloadDresser"/> needs to dress one topic's example payload.</summary>
public sealed class TestPayloadDressingContext
{
    /// <summary>Initializes a new instance.</summary>
    public TestPayloadDressingContext(
        string topic,
        IReadOnlyDictionary<string, string> headers,
        string serializedBody,
        IReadOnlyList<string> transports,
        IReadOnlyList<TestPayloadHttpMapping> httpMappings)
    {
        Topic = topic;
        Headers = headers;
        SerializedBody = serializedBody;
        Transports = transports;
        HttpMappings = httpMappings;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>Message headers to carry (the core seeds none; a dresser may add transport-specific ones).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// The deterministic example message body, already serialized to a JSON string - the same body the
    /// <c>benzene-message</c> envelope carries, so every transport's dressed payload agrees on it.
    /// </summary>
    public string SerializedBody { get; }

    /// <summary>The transports this topic is reachable on, so a dresser can skip one the host isn't wired for.</summary>
    public IReadOnlyList<string> Transports { get; }

    /// <summary>The topic's HTTP method/path mappings (empty when it has none).</summary>
    public IReadOnlyList<TestPayloadHttpMapping> HttpMappings { get; }

    /// <summary>Whether the host advertises <paramref name="transport"/> for this topic (case-insensitive).</summary>
    public bool SupportsTransport(string transport)
    {
        foreach (var t in Transports)
        {
            if (string.Equals(t, transport, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
