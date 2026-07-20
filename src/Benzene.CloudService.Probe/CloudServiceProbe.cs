using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benzene.CloudService.Probe;

/// <summary>
/// Runs an external, black-box conformance probe against a live Benzene Cloud Service
/// (docs/specification/cloud-service-profile.md), asserting R1-R8's observable surfaces from
/// outside - health response shape, envelope round-trip, spec and descriptor presence, default
/// paths - over real HTTP, without relying on anything the service says about itself.
///
/// This is intentionally decoupled from <c>Benzene.CloudService</c> (see
/// <see cref="CloudServiceProbePaths"/>'s remarks): it works against any service that speaks the
/// wire contracts, .NET or not.
/// </summary>
public static class CloudServiceProbe
{
    private const string HealthDescription = "Health checks";
    private const string InvokeDescription = "Wire-envelope invocability";
    private const string SpecDescription = "Derived spec";
    private const string MeshDescription = "Mesh service-side feeds (observable half - see reason)";

    private const string HealthcheckEnvelope = "{\"topic\":\"healthcheck\",\"headers\":{},\"body\":\"{}\"}";
    private const string MeshEnvelope = "{\"topic\":\"mesh\",\"headers\":{},\"body\":\"{}\"}";

    /// <summary>
    /// Probes the service at <paramref name="httpClient"/>'s <c>BaseAddress</c> and returns a
    /// tri-state assessment of R1-R8. Never throws for an unreachable or non-conformant service -
    /// unreachability and shape mismatches are reported as <see cref="CloudServiceProbeVerdict.NotSatisfied"/>
    /// or <see cref="CloudServiceProbeVerdict.Inconclusive"/> verdicts, not exceptions.
    /// </summary>
    /// <param name="httpClient">
    /// The client to probe with; its <c>BaseAddress</c> is the target service. The caller owns its
    /// lifetime (timeout, base address, any auth headers needed to reach the service).
    /// </param>
    /// <param name="options">Path overrides and the R8 bonus-signal toggle; defaults match the profile's /benzene/* standard.</param>
    /// <param name="cancellationToken">Cancels the run; an externally-requested cancellation propagates, an internal per-request timeout does not.</param>
    public static async Task<CloudServiceProbeReport> RunAsync(
        HttpClient httpClient,
        CloudServiceProbeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (httpClient is null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        options ??= new CloudServiceProbeOptions();

        var health = await ProbeHealthAsync(httpClient, options, cancellationToken).ConfigureAwait(false);
        var invoke = await ProbeInvokeAsync(httpClient, options, cancellationToken).ConfigureAwait(false);
        var spec = await ProbeSpecAsync(httpClient, options, cancellationToken).ConfigureAwait(false);
        var mesh = await ProbeMeshAsync(httpClient, options, cancellationToken).ConfigureAwait(false);

        var requirements = new List<CloudServiceProbeRequirement>
        {
            EvaluateR1(health, invoke, spec, mesh),
            EvaluateR2(mesh),
            health.Requirement,
            invoke.Requirement,
            spec.Requirement,
            mesh.Requirement,
            EvaluateR7(options, health, invoke, spec, mesh),
            EvaluateR8(options, invoke, mesh)
        };

        return new CloudServiceProbeReport(requirements);
    }

    // ---- R3: GET the health path; satisfied iff 200 OR 503 with a boolean isHealthy field (wire-contracts.md §5). ----
    private static async Task<(CloudServiceProbeRequirement Requirement, bool Reached)> ProbeHealthAsync(
        HttpClient client, CloudServiceProbeOptions options, CancellationToken ct)
    {
        var (reached, status, body, failure) = await GetAsync(client, options.HealthPath, ct).ConfigureAwait(false);
        if (!reached)
        {
            return (new CloudServiceProbeRequirement("R3", HealthDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"GET {options.HealthPath} did not reach the service: {failure}"), false);
        }
        // A conformant service returns 200 when healthy and 503 when unhealthy - both carry the same
        // health-report body (see Benzene.HealthChecks.HealthCheckProcessor, which sets the 503 as a
        // successful result so the report still renders). Runtime degradation is NOT a conformance
        // failure (cloud-service-profile.md §4), so accept either and judge R3 on the report shape -
        // matching how the mesh aggregator reads a 503 body rather than treating it as a fetch failure.
        if (status != 200 && status != 503)
        {
            return (new CloudServiceProbeRequirement("R3", HealthDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"GET {options.HealthPath} returned {status}, expected 200 or 503"), true);
        }
        if (!TryGetBoolField(body, "isHealthy", out _))
        {
            return (new CloudServiceProbeRequirement("R3", HealthDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"{status} response at {options.HealthPath} did not have a boolean 'isHealthy' field (wire-contracts.md §5)"), true);
        }
        return (new CloudServiceProbeRequirement("R3", HealthDescription, CloudServiceProbeVerdict.Satisfied,
            $"GET {options.HealthPath} returned {status} with a boolean 'isHealthy' field"), true);
    }

    // ---- R4: POST a synthetic envelope; satisfied iff 200 with the {statusCode, headers, body} shape (wire-contracts.md §1.2). ----
    private static async Task<(CloudServiceProbeRequirement Requirement, bool Reached)> ProbeInvokeAsync(
        HttpClient client, CloudServiceProbeOptions options, CancellationToken ct)
    {
        var (reached, status, body, failure) = await PostAsync(
            client, options.InvokePath, HealthcheckEnvelope, options.SendTraceParentProbe, ct).ConfigureAwait(false);
        if (!reached)
        {
            return (new CloudServiceProbeRequirement("R4", InvokeDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"POST {options.InvokePath} did not reach the service: {failure}"), false);
        }
        if (status != 200)
        {
            return (new CloudServiceProbeRequirement("R4", InvokeDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"POST {options.InvokePath} with a synthetic envelope returned {status}, expected 200"), true);
        }
        if (!TryParseEnvelopeResponse(body, out _, out var shapeReason))
        {
            return (new CloudServiceProbeRequirement("R4", InvokeDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"200 response at {options.InvokePath} did not have the {{statusCode, headers, body}} envelope shape (wire-contracts.md §1.2): {shapeReason}"), true);
        }
        return (new CloudServiceProbeRequirement("R4", InvokeDescription, CloudServiceProbeVerdict.Satisfied,
            $"POST {options.InvokePath} with a synthetic envelope round-tripped a valid {{statusCode, headers, body}} response"), true);
    }

    // ---- R5: GET the spec path; satisfied iff 200 with a non-empty JSON object. No specific spec format is assumed. ----
    private static async Task<(CloudServiceProbeRequirement Requirement, bool Reached)> ProbeSpecAsync(
        HttpClient client, CloudServiceProbeOptions options, CancellationToken ct)
    {
        var (reached, status, body, failure) = await GetAsync(client, options.SpecPath, ct).ConfigureAwait(false);
        if (!reached)
        {
            return (new CloudServiceProbeRequirement("R5", SpecDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"GET {options.SpecPath} did not reach the service: {failure}"), false);
        }
        if (status != 200)
        {
            return (new CloudServiceProbeRequirement("R5", SpecDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"GET {options.SpecPath} returned {status}, expected 200"), true);
        }
        if (!TryParseNonEmptyObject(body))
        {
            return (new CloudServiceProbeRequirement("R5", SpecDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"200 response at {options.SpecPath} did not parse as a non-empty JSON object. The profile does not mandate a specific spec format (OpenAPI, AsyncAPI, or Benzene's own), only that a real derived document is there"), true);
        }
        return (new CloudServiceProbeRequirement("R5", SpecDescription, CloudServiceProbeVerdict.Satisfied,
            $"GET {options.SpecPath} returned 200 with a non-empty JSON object body"), true);
    }

    // ---- R6 (observable half only): POST topic "mesh"; satisfied iff 200 envelope wraps a descriptor with a non-empty "service" (mesh.md §2). ----
    private static async Task<(CloudServiceProbeRequirement Requirement, bool Reached, JsonArray? Topics)> ProbeMeshAsync(
        HttpClient client, CloudServiceProbeOptions options, CancellationToken ct)
    {
        const string caveat =
            "Registration (mesh:register) and heartbeat (mesh:heartbeat) delivery to a collector cannot be observed " +
            "by probing the service alone, so that half of R6 stays unverified even when the descriptor endpoint " +
            "itself checks out - a passing descriptor check does not imply the whole of R6.";

        var (reached, status, body, failure) = await PostAsync(
            client, options.InvokePath, MeshEnvelope, options.SendTraceParentProbe, ct).ConfigureAwait(false);
        if (!reached)
        {
            return (new CloudServiceProbeRequirement("R6", MeshDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"POST {options.InvokePath} with topic 'mesh' did not reach the service: {failure}. {caveat}"), false, null);
        }
        if (status != 200)
        {
            return (new CloudServiceProbeRequirement("R6", MeshDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"POST {options.InvokePath} with topic 'mesh' returned {status}, expected 200 (the reserved mesh topic does not appear to be served). {caveat}"), true, null);
        }
        if (!TryParseEnvelopeResponse(body, out var innerBody, out var envelopeReason))
        {
            return (new CloudServiceProbeRequirement("R6", MeshDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"200 response to the 'mesh' topic did not have the {{statusCode, headers, body}} envelope shape (wire-contracts.md §1.2): {envelopeReason}. {caveat}"), true, null);
        }
        if (!TryParseDescriptor(innerBody, out var descriptor, out var descriptorReason))
        {
            return (new CloudServiceProbeRequirement("R6", MeshDescription, CloudServiceProbeVerdict.NotSatisfied,
                $"200 response to the 'mesh' topic did not parse as a descriptor with a non-empty 'service' field (mesh.md §2): {descriptorReason}. {caveat}"), true, null);
        }

        var topics = descriptor!["topics"] as JsonArray;
        return (new CloudServiceProbeRequirement("R6", MeshDescription, CloudServiceProbeVerdict.Satisfied,
            $"the reserved 'mesh' topic served a descriptor for service '{descriptor["service"]}' - the observable half of R6 checks out. {caveat}"), true, topics);
    }

    // ---- R1: inferential - satisfied iff anything responded at all, since that proves a hosted pipeline exists. ----
    private static CloudServiceProbeRequirement EvaluateR1(
        (CloudServiceProbeRequirement Requirement, bool Reached) health,
        (CloudServiceProbeRequirement Requirement, bool Reached) invoke,
        (CloudServiceProbeRequirement Requirement, bool Reached) spec,
        (CloudServiceProbeRequirement Requirement, bool Reached, JsonArray? Topics) mesh)
    {
        const string description = "Hosted middleware pipeline";
        var anyReached = health.Reached || invoke.Reached || spec.Reached || mesh.Reached;
        return anyReached
            ? new CloudServiceProbeRequirement("R1", description, CloudServiceProbeVerdict.Satisfied,
                "at least one probed surface responded over HTTP, which proves a hosted pipeline exists behind a transport binding - an in-process-only pipeline would have nothing for this probe to reach")
            : new CloudServiceProbeRequirement("R1", description, CloudServiceProbeVerdict.NotSatisfied,
                "none of the health, invoke, spec, or mesh surfaces responded at all; no hosted pipeline was reachable at the configured base address");
    }

    // ---- R2: inferential from R6's descriptor topics only - the profile doesn't mandate a spec format, so topic counts can't be read from R5. ----
    private static CloudServiceProbeRequirement EvaluateR2(
        (CloudServiceProbeRequirement Requirement, bool Reached, JsonArray? Topics) mesh)
    {
        const string description = "Message handlers via the registry";
        if (mesh.Requirement.Verdict != CloudServiceProbeVerdict.Satisfied)
        {
            return new CloudServiceProbeRequirement("R2", description, CloudServiceProbeVerdict.Inconclusive,
                "no reachable mesh descriptor to read registered topics from, and the profile does not mandate a spec document format, so topic counts can't be read from the spec surface either - absence of evidence isn't evidence of absence");
        }
        if (mesh.Topics is { Count: > 0 })
        {
            return new CloudServiceProbeRequirement("R2", description, CloudServiceProbeVerdict.Satisfied,
                $"the mesh descriptor lists {mesh.Topics.Count} registered topic(s) (mesh.md §2), which can only be populated from the handler registry");
        }
        return new CloudServiceProbeRequirement("R2", description, CloudServiceProbeVerdict.Inconclusive,
            "the mesh descriptor is reachable but lists no topics; a service can legitimately have zero handlers registered so far - absence of evidence isn't evidence of absence");
    }

    // ---- R7: only assessable when the probe itself used the defaults; otherwise it can't tell what the service defaults to. ----
    private static CloudServiceProbeRequirement EvaluateR7(
        CloudServiceProbeOptions options,
        (CloudServiceProbeRequirement Requirement, bool Reached) health,
        (CloudServiceProbeRequirement Requirement, bool Reached) invoke,
        (CloudServiceProbeRequirement Requirement, bool Reached) spec,
        (CloudServiceProbeRequirement Requirement, bool Reached, JsonArray? Topics) mesh)
    {
        const string description = "Default service standard paths";
        if (!options.UsesDefaultPaths)
        {
            return new CloudServiceProbeRequirement("R7", description, CloudServiceProbeVerdict.Inconclusive,
                "the probe was configured with non-default path(s), so it cannot tell whether the service itself defaults to /benzene/invoke, /benzene/spec, and /benzene/health - it was told to look elsewhere");
        }

        var failing = new List<string>();
        if (health.Requirement.Verdict != CloudServiceProbeVerdict.Satisfied) failing.Add("R3 (health)");
        if (invoke.Requirement.Verdict != CloudServiceProbeVerdict.Satisfied) failing.Add("R4 (invoke)");
        if (spec.Requirement.Verdict != CloudServiceProbeVerdict.Satisfied) failing.Add("R5 (spec)");
        if (mesh.Requirement.Verdict != CloudServiceProbeVerdict.Satisfied) failing.Add("R6 (mesh)");

        if (failing.Count == 0)
        {
            return new CloudServiceProbeRequirement("R7", description, CloudServiceProbeVerdict.Satisfied,
                "the probe used the spec's /benzene/* default paths and R3-R6 all checked out there");
        }
        return new CloudServiceProbeRequirement("R7", description, CloudServiceProbeVerdict.NotSatisfied,
            $"the probe used the spec's /benzene/* default paths, but {string.Join(", ", failing)} did not check out there");
    }

    // ---- R8: never observable by a single-service black-box probe; always Inconclusive, with an optional labeled bonus signal. ----
    private static CloudServiceProbeRequirement EvaluateR8(
        CloudServiceProbeOptions options,
        (CloudServiceProbeRequirement Requirement, bool Reached) invoke,
        (CloudServiceProbeRequirement Requirement, bool Reached, JsonArray? Topics) mesh)
    {
        const string description = "Trace context join and propagation";
        const string baseReason =
            "trace context propagation cannot be verified by a single-service black-box HTTP probe; proving it " +
            "requires either a second service to observe forwarded traceparent headers, or a mesh collector " +
            "deriving consumer edges from trace parentage (mesh.md §3-4)";

        if (!options.SendTraceParentProbe)
        {
            return new CloudServiceProbeRequirement("R8", description, CloudServiceProbeVerdict.Inconclusive, baseReason);
        }

        var nonBreaking = invoke.Requirement.Verdict == CloudServiceProbeVerdict.Satisfied &&
                           mesh.Requirement.Verdict == CloudServiceProbeVerdict.Satisfied;
        var bonus = nonBreaking
            ? " (weak bonus signal: the service still responded correctly with a synthetic traceparent header attached to the R4/R6 calls - this is non-breakage only, not proof of propagation, and does not upgrade this verdict)"
            : " (a bonus traceparent header was attached to the R4/R6 calls, but at least one of those calls did not succeed, so even the weak non-breakage signal is absent)";

        return new CloudServiceProbeRequirement("R8", description, CloudServiceProbeVerdict.Inconclusive, baseReason + bonus);
    }

    private static async Task<(bool Reached, int? StatusCode, string? Body, string? FailureReason)> GetAsync(
        HttpClient client, string path, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync(path, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return (true, (int)response.StatusCode, body, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (false, null, null, "request timed out");
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    private static async Task<(bool Reached, int? StatusCode, string? Body, string? FailureReason)> PostAsync(
        HttpClient client, string path, string jsonContent, bool includeTraceParent, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            if (includeTraceParent)
            {
                request.Headers.TryAddWithoutValidation("traceparent", GenerateSyntheticTraceParent());
            }
            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return (true, (int)response.StatusCode, body, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (false, null, null, "request timed out");
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    /// <summary>A syntactically-valid, random W3C traceparent (https://www.w3.org/TR/trace-context/#traceparent-header) for the R8 bonus probe.</summary>
    private static string GenerateSyntheticTraceParent()
    {
        Span<byte> traceId = stackalloc byte[16];
        Span<byte> spanId = stackalloc byte[8];
        RandomNumberGenerator.Fill(traceId);
        RandomNumberGenerator.Fill(spanId);
        return $"00-{Convert.ToHexString(traceId).ToLowerInvariant()}-{Convert.ToHexString(spanId).ToLowerInvariant()}-01";
    }

    private static bool TryGetBoolField(string? raw, string fieldName, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return false;
        }
        if (node is not JsonObject obj || obj[fieldName] is not JsonValue field)
        {
            return false;
        }
        var kind = field.GetValueKind();
        if (kind != JsonValueKind.True && kind != JsonValueKind.False)
        {
            return false;
        }
        value = field.GetValue<bool>();
        return true;
    }

    private static bool TryParseNonEmptyObject(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return false;
        }
        return node is JsonObject { Count: > 0 };
    }

    /// <summary>Validates the {statusCode, headers, body} response envelope shape (wire-contracts.md §1.2) and extracts the pre-serialized body string.</summary>
    private static bool TryParseEnvelopeResponse(string? raw, out string? innerBody, out string? reason)
    {
        innerBody = null;
        reason = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            reason = "empty response body";
            return false;
        }
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (JsonException ex)
        {
            reason = $"not valid JSON: {ex.Message}";
            return false;
        }
        if (node is not JsonObject obj)
        {
            reason = "not a JSON object";
            return false;
        }
        if (obj["statusCode"] is not JsonValue sc || sc.GetValueKind() != JsonValueKind.String)
        {
            reason = "missing a string 'statusCode' field";
            return false;
        }
        if (obj["headers"] is not JsonObject)
        {
            reason = "missing an object 'headers' field";
            return false;
        }
        if (obj["body"] is not JsonValue bodyValue || bodyValue.GetValueKind() != JsonValueKind.String)
        {
            reason = "missing a string 'body' field";
            return false;
        }
        innerBody = bodyValue.GetValue<string>();
        return true;
    }

    /// <summary>Parses an envelope's inner body as a ServiceDescriptor (mesh.md §2) and requires a non-empty "service" field.</summary>
    private static bool TryParseDescriptor(string? innerBody, out JsonObject? descriptor, out string? reason)
    {
        descriptor = null;
        reason = null;
        if (string.IsNullOrWhiteSpace(innerBody))
        {
            reason = "empty envelope body";
            return false;
        }
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(innerBody);
        }
        catch (JsonException ex)
        {
            reason = $"envelope body is not valid JSON: {ex.Message}";
            return false;
        }
        if (node is not JsonObject obj)
        {
            reason = "envelope body is not a JSON object";
            return false;
        }
        if (obj["service"] is not JsonValue sv || sv.GetValueKind() != JsonValueKind.String || string.IsNullOrEmpty(sv.GetValue<string>()))
        {
            reason = "missing a non-empty string 'service' field";
            return false;
        }
        descriptor = obj;
        return true;
    }
}
