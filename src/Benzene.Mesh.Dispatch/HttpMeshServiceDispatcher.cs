using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Dispatch;

/// <summary>
/// Dispatches to an HTTP-reachable service by POSTing the Benzene message envelope
/// (<c>{ topic, headers, body }</c>) to its wire-envelope endpoint. The endpoint URL comes from the
/// entry's <c>SourceOptions["invokeUrl"]</c> when present, otherwise it's derived from the entry's
/// <see cref="MeshServiceRegistryEntry.SpecUrl"/> origin as <c>&lt;origin&gt;/benzene-message</c>
/// (Benzene's default receiving path).
/// </summary>
public class HttpMeshServiceDispatcher : IMeshServiceDispatcher
{
    /// <summary>The <see cref="MeshServiceRegistryEntry.SourceOptions"/> key overriding the invoke URL.</summary>
    public const string InvokeUrlOption = "invokeUrl";
    private const string DefaultInvokePath = "/benzene-message";

    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="HttpMeshServiceDispatcher"/> class.</summary>
    public HttpMeshServiceDispatcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public string Key => MeshServiceSource.Http;

    /// <inheritdoc />
    public async Task<MeshDispatchResult> DispatchAsync(MeshServiceRegistryEntry entry, MeshDispatchEnvelope envelope, CancellationToken cancellationToken)
    {
        var url = ResolveInvokeUrl(entry);
        var payload = JsonSerializer.Serialize(new
        {
            topic = envelope.Topic,
            headers = envelope.Headers,
            body = envelope.Body,
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // Pass back exactly what the service returned. A non-2xx HTTP status still carries a body.
        return new MeshDispatchResult(((int)response.StatusCode).ToString(), responseBody);
    }

    private static string ResolveInvokeUrl(MeshServiceRegistryEntry entry)
    {
        if (entry.SourceOptions != null
            && entry.SourceOptions.TryGetValue(InvokeUrlOption, out var explicitUrl)
            && !string.IsNullOrWhiteSpace(explicitUrl))
        {
            return explicitUrl;
        }

        if (string.IsNullOrWhiteSpace(entry.SpecUrl))
        {
            throw new InvalidOperationException(
                $"Mesh service \"{entry.Name}\" has no \"{InvokeUrlOption}\" in SourceOptions and no SpecUrl to derive an invoke URL from.");
        }

        var origin = new Uri(entry.SpecUrl).GetLeftPart(UriPartial.Authority);
        return origin + DefaultInvokePath;
    }
}
