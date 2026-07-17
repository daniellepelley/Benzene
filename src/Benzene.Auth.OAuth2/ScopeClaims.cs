using System.Security.Claims;
using System.Text.Json;

namespace Benzene.Auth.OAuth2;

/// <summary>
/// Reads the granted-scopes set off a <see cref="ClaimsPrincipal"/>, normalizing the two
/// conventions real-world OAuth2/JWT issuers use (per the design doc's §8 Q1 decision - support
/// both, don't pick one): RFC 8693's single space-delimited <c>scope</c> claim, and Azure AD's
/// <c>scp</c> claim, which itself appears as either a space-delimited string or a JSON array
/// depending on issuer.
/// </summary>
internal static class ScopeClaims
{
    private const string ScopeClaimType = "scope";
    private const string ScpClaimType = "scp";

    /// <summary>
    /// Returns the flat set of scope strings granted to <paramref name="principal"/>, merging
    /// every <c>scope</c> and <c>scp</c> claim value present.
    /// </summary>
    public static HashSet<string> GetGrantedScopes(ClaimsPrincipal principal)
    {
        var granted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in principal.FindAll(ScopeClaimType))
        {
            AddSpaceDelimited(granted, claim.Value);
        }

        foreach (var claim in principal.FindAll(ScpClaimType))
        {
            AddScpValue(granted, claim.Value);
        }

        return granted;
    }

    private static void AddSpaceDelimited(HashSet<string> granted, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var scope in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            granted.Add(scope);
        }
    }

    private static void AddScpValue(HashSet<string> granted, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.TrimStart().StartsWith('['))
        {
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(value);
                if (array != null)
                {
                    foreach (var scope in array)
                    {
                        if (!string.IsNullOrEmpty(scope))
                        {
                            granted.Add(scope);
                        }
                    }
                    return;
                }
            }
            catch (JsonException)
            {
                // Not actually valid JSON despite looking like it - fall through and treat it as
                // a plain (space-delimited) string instead of silently discarding the claim.
            }
        }

        AddSpaceDelimited(granted, value);
    }
}
