using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Benzene.Mesh.Wire;

/// <summary>
/// One deduplicated, classified failure signature (spec §4.1) — the pipeline-native answer to
/// "what is wrong", where a trace answers "what happened". Sparse by construction: emitted on
/// failure only, fingerprint-deduplicated at source.
/// </summary>
public class MeshIssue
{
    /// <summary>Normative identity (spec §4.1): lowercase hex of the first 16 bytes of SHA-256 over
    /// <c>service|topic|version|classification|discriminator</c> — see <see cref="MeshIssueFingerprint"/>.</summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>One of <see cref="MeshIssueClassification"/>'s closed vocabulary.</summary>
    public string Classification { get; set; } = string.Empty;

    public string Service { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>Descriptive only — deliberately NOT part of the fingerprint (the same failure over
    /// two transports is one issue).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Transport { get; set; }

    /// <summary>The Benzene status verbatim (wire-contracts §3).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The thrown exception's language-native type name — never a message or stack trace.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionType { get; set; }

    /// <summary>DELTA: occurrences since the emitter's previous successful flush, never cumulative
    /// (spec §4.1 — deltas merge restart-proof with no instance identity on the wire).</summary>
    public long Count { get; set; }

    public DateTimeOffset FirstSeen { get; set; }

    public DateTimeOffset LastSeen { get; set; }

    /// <summary>The newest observed trace ids for this issue (≤3) — the bridge to the evidence plane.</summary>
    public List<string> ExemplarTraceIds { get; set; } = new();

    /// <summary>Optional key into a remediation catalog (spec §4.1 registered keys: <c>no-handler</c>,
    /// <c>deserialization</c>) — never prose. Unknown keys are tolerated by readers.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResolutionHint { get; set; }
}

/// <summary>The body of a <c>mesh:issues</c> message (spec §4.1): one emitter flush. The batch-level
/// <see cref="Service"/> is REQUIRED even though each issue carries its own — an EMPTY batch is the
/// feed's liveness assertion ("feed alive, nothing failing") and must be attributable.</summary>
public class MeshIssueBatch
{
    public string Service { get; set; } = string.Empty;
    public List<MeshIssue> Issues { get; set; } = new();
}

/// <summary>The closed classification vocabulary and its normative precedence table (spec §4.1).</summary>
public static class MeshIssueClassification
{
    public const string Exception = "exception";
    public const string Validation = "validation";
    public const string ConfigWiring = "config-wiring";
    public const string Dependency = "dependency";
    /// <summary>Reserved for catalog/heartbeat-derived issues (hash mismatch, schema divergence) filed
    /// by a collector or reader — never produced by <see cref="Classify"/>.</summary>
    public const string ContractDrift = "contract-drift";
    public const string Unclassified = "unclassified";

    /// <summary>The spec §4.1 precedence table, evaluated in order. <paramref name="status"/> is the
    /// invocation's Benzene status (empty = the §3 wiring gap); <paramref name="exceptionType"/> is the
    /// captured thrown-exception type, when any.</summary>
    public static string Classify(string? status, string? exceptionType)
    {
        switch (status)
        {
            // 1. Validation statuses classify as validation even when an exception was thrown (a
            //    deserialization/argument exception IS the validation failure; the type still serves
            //    as the fingerprint discriminator).
            case "bad-request":
            case "validation-error":
                return Validation;
        }

        // 2. A thrown-and-converted failure classifies by its throw, not its mapped status — this is
        //    what stops every thrown NullReferenceException (mapped to service-unavailable) from
        //    reading as a dependency problem.
        if (!string.IsNullOrEmpty(exceptionType))
        {
            return Exception;
        }

        switch (status)
        {
            // 3. Wiring/config drift at this boundary (an empty status is normatively a wiring gap).
            case "not-found":
            case "unauthorized":
            case "forbidden":
            case "not-implemented":
            case null:
            case "":
                return ConfigWiring;

            // 4. Something this service calls is broken (reached only when nothing was thrown —
            //    i.e. an explicit handler result or a client-side send failure).
            case "service-unavailable":
            case "timeout":
            case "too-many-requests":
                return Dependency;

            // 5. The wire's own "unclassified failure" status originates from generic error mapping.
            case "unexpected-error":
            case "exception":
                return Exception;

            // 6. An honest fallback beats a lying class (conflict, app-custom statuses, ...).
            default:
                return Unclassified;
        }
    }
}

/// <summary>The normative fingerprint derivation (spec §4.1).</summary>
public static class MeshIssueFingerprint
{
    /// <summary>Lowercase hex of the first 16 bytes of SHA-256 over the UTF-8 bytes of
    /// <c>service|topic|version|classification|discriminator</c>, where <paramref name="version"/> is
    /// the empty string when absent and the discriminator is <paramref name="exceptionType"/> when
    /// present, else <paramref name="status"/>. Transport is deliberately excluded.</summary>
    public static string Compute(string service, string topic, string? version, string classification,
        string? exceptionType, string? status)
    {
        var discriminator = !string.IsNullOrEmpty(exceptionType) ? exceptionType : (status ?? string.Empty);
        var canonical = service + "|" + topic + "|" + (version ?? string.Empty) + "|" + classification + "|" + discriminator;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }
}
