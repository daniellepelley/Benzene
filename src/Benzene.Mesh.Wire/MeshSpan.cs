using System.Security.Cryptography;

namespace Benzene.Mesh.Wire;

/// <summary>
/// The current invocation's position in a trace: the ids the trace middleware assigned (or adopted
/// from the caller's traceparent) for the event it will export. A handler making a downstream
/// Benzene call forwards <see cref="ToTraceparent"/> as the <c>traceparent</c> header
/// (docs/specification/mesh.md §3) - the join that lets a collector derive consumer edges from
/// parentage. Flows via <see cref="MeshSpan.Current"/> (AsyncLocal, the CLR idiom for
/// invocation-ambient state).
/// </summary>
public class MeshSpan
{
    private static readonly AsyncLocal<MeshSpan?> Ambient = new();

    public MeshSpan(string traceId, string spanId)
    {
        TraceId = traceId;
        SpanId = spanId;
    }

    /// <summary>The traced invocation currently executing on this async flow, or null when unmeshed
    /// - per the spec's degradation rule, a caller then simply sends no traceparent header.</summary>
    public static MeshSpan? Current
    {
        get => Ambient.Value;
        internal set => Ambient.Value = value;
    }

    public string TraceId { get; }

    public string SpanId { get; }

    /// <summary>Renders the span as a W3C traceparent header value: version 00, this span as the
    /// parent-id, sampled flag set.</summary>
    public string ToTraceparent() => $"00-{TraceId}-{SpanId}-01";
}

/// <summary>
/// W3C traceparent parsing per docs/specification/mesh.md §3: absent or malformed values (wrong
/// segment count/length, non-hex, or the all-zero ids the W3C spec defines as invalid) yield no
/// ids, and the trace middleware starts a fresh trace - a bad caller header degrades correlation,
/// never the invocation.
/// </summary>
public static class Traceparent
{
    public static bool TryParse(string? header, out string traceId, out string parentSpanId)
    {
        traceId = string.Empty;
        parentSpanId = string.Empty;
        if (string.IsNullOrEmpty(header))
        {
            return false;
        }

        var parts = header.Split('-');
        if (parts.Length != 4 ||
            parts[0].Length != 2 || parts[1].Length != 32 || parts[2].Length != 16 || parts[3].Length != 2 ||
            !IsLowerHex(parts[0]) || !IsLowerHex(parts[1]) || !IsLowerHex(parts[2]) || !IsLowerHex(parts[3]) ||
            parts[1] == new string('0', 32) || parts[2] == new string('0', 16))
        {
            return false;
        }

        traceId = parts[1];
        parentSpanId = parts[2];
        return true;
    }

    /// <summary>A fresh random id of <paramref name="bytes"/> bytes, lowercase hex. Trace/span ids
    /// need uniqueness, not unpredictability.</summary>
    public static string NewId(int bytes)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
    }

    private static bool IsLowerHex(string value)
    {
        foreach (var c in value)
        {
            if (c is not ((>= '0' and <= '9') or (>= 'a' and <= 'f')))
            {
                return false;
            }
        }
        return true;
    }
}
