using System.Globalization;

namespace Benzene.Mesh.Collector;

/// <summary>
/// Resolves a requested <see cref="MeshTimeRange"/> (Grafana relative grammar or ISO-8601 absolute) into a
/// concrete absolute <c>[From,To]</c> window against a supplied <c>now</c>, and helps read models build the
/// <see cref="MeshWindow"/> they report. A null range - or a range whose <see cref="MeshTimeRange.From"/> is
/// null/empty (no lower bound) - resolves to null: "unfiltered", today's behavior, so old clients and the
/// conformance fixtures are unaffected.
/// </summary>
/// <remarks>
/// The relative grammar mirrors Grafana's: <c>now</c>, and <c>now</c> followed by <c>-</c>/<c>+</c>, an integer,
/// and a unit - <c>s</c> seconds, <c>m</c> minutes, <c>h</c> hours, <c>d</c> days, <c>w</c> weeks, <c>M</c>
/// months (~30d), <c>y</c> years (~365d). A trailing <c>/unit</c> rounding suffix (e.g. <c>now-1d/d</c>) is
/// accepted and ignored (the rounding is a UI nicety the read models don't need). Anything else is parsed as
/// an ISO-8601 absolute instant; an unparseable bound is treated as absent.
/// </remarks>
public static class MeshTimeRangeResolver
{
    /// <summary>Resolve the range to an absolute <c>[From,To]</c>, or null when it is absent/unfiltered.</summary>
    public static (DateTimeOffset From, DateTimeOffset To)? Resolve(MeshTimeRange? range, DateTimeOffset now)
    {
        if (range == null)
        {
            return null;
        }

        var from = ParseBound(range.From, now);
        if (from == null)
        {
            // No lower bound → unfiltered (an upper bound alone doesn't make a window on these read models).
            return null;
        }

        var to = ParseBound(range.To, now) ?? now;
        return (from.Value, to);
    }

    /// <summary>Format an instant as the ISO-8601 UTC string the wire <see cref="MeshWindow"/> uses.</summary>
    public static string ToIso(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static DateTimeOffset? ParseBound(string? value, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            return now;
        }

        if (value.StartsWith("now", StringComparison.OrdinalIgnoreCase))
        {
            var rest = value.Substring(3);
            // Drop an optional rounding suffix (now-1d/d) - the read models don't need the rounding.
            var slash = rest.IndexOf('/');
            if (slash >= 0)
            {
                rest = rest.Substring(0, slash);
            }

            if (rest.Length == 0)
            {
                return now;
            }

            var sign = rest[0];
            if (sign != '-' && sign != '+')
            {
                return null;
            }

            var span = ParseDuration(rest.Substring(1));
            if (span == null)
            {
                return null;
            }

            return sign == '-' ? now - span.Value : now + span.Value;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var absolute)
            ? absolute
            : null;
    }

    private static TimeSpan? ParseDuration(string s)
    {
        if (s.Length < 2)
        {
            return null;
        }

        var unit = s[^1];
        if (!long.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return null;
        }

        // 'm' is minutes, 'M' is months - the case distinction is deliberate (Grafana's grammar).
        return unit switch
        {
            's' => TimeSpan.FromSeconds(n),
            'm' => TimeSpan.FromMinutes(n),
            'h' => TimeSpan.FromHours(n),
            'd' => TimeSpan.FromDays(n),
            'w' => TimeSpan.FromDays(n * 7),
            'M' => TimeSpan.FromDays(n * 30),
            'y' => TimeSpan.FromDays(n * 365),
            _ => null
        };
    }
}
