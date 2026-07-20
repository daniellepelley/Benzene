namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>The request for the <c>test-payloads</c> topic.</summary>
public class TestPayloadsRequest
{
    /// <summary>When set, restricts the manifest to this single topic; otherwise every domain topic is returned.</summary>
    public string? Topic { get; set; }
}
