using System.Security.Claims;
using System.Text.Json;

namespace Benzene.Auth.Core;

/// <summary>
/// Reads the granted-role set off a <see cref="ClaimsPrincipal"/>, normalizing the claim shapes
/// real-world issuers use for roles: the BCL default (<see cref="ClaimTypes.Role"/>, which
/// <see cref="ClaimsPrincipal.IsInRole"/> reads), and the bare <c>role</c>/<c>roles</c> claims
/// common in JWTs (Azure AD app roles arrive in a <c>roles</c> claim as a JSON array).
/// </summary>
/// <remarks>
/// Unlike OAuth2 scopes (which are always space-delimited within a single claim, see
/// <c>Benzene.Auth.OAuth2</c>'s <c>ScopeClaims</c>), role names can themselves contain spaces, so a
/// role claim value is never space-split — each claim value is one role, except a JSON-array value
/// which is expanded element by element.
/// </remarks>
internal static class RoleClaims
{
    private static readonly string[] RoleClaimTypes = { ClaimTypes.Role, "role", "roles" };

    /// <summary>
    /// Returns whether <paramref name="principal"/> holds at least one of <paramref name="anyOfRoles"/>,
    /// checking both the principal's own <see cref="ClaimsPrincipal.IsInRole"/> (which honors each
    /// identity's configured <c>RoleClaimType</c>) and the normalized role set from the common role
    /// claim types.
    /// </summary>
    public static bool IsInAnyRole(ClaimsPrincipal principal, IReadOnlyCollection<string> anyOfRoles)
    {
        foreach (var role in anyOfRoles)
        {
            if (principal.IsInRole(role))
            {
                return true;
            }
        }

        var granted = GetGrantedRoles(principal);
        return anyOfRoles.Any(granted.Contains);
    }

    /// <summary>Returns the flat set of role strings granted to <paramref name="principal"/>.</summary>
    public static HashSet<string> GetGrantedRoles(ClaimsPrincipal principal)
    {
        var granted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var claimType in RoleClaimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                AddValue(granted, claim.Value);
            }
        }

        return granted;
    }

    private static void AddValue(HashSet<string> granted, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // A "roles" claim can arrive as a JSON array (Azure AD's app-roles convention) rather than
        // one claim per role - expand it. Everything else is a single role value, added verbatim.
        if (value.TrimStart().StartsWith('['))
        {
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(value);
                if (array != null)
                {
                    foreach (var role in array)
                    {
                        if (!string.IsNullOrWhiteSpace(role))
                        {
                            granted.Add(role.Trim());
                        }
                    }

                    return;
                }
            }
            catch (JsonException)
            {
                // Not actually valid JSON despite the leading bracket - fall through and treat the
                // whole value as one role rather than silently discarding the claim.
            }
        }

        granted.Add(value.Trim());
    }
}
