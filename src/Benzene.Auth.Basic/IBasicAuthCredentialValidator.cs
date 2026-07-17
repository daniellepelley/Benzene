using System.Security.Claims;

namespace Benzene.Auth.Basic;

/// <summary>
/// Validates the username/password pair carried by an RFC 7617 <c>Authorization: Basic</c>
/// header. Implement this against whatever credential store the app actually uses (a secrets
/// manager, an env var for a single service account, a user table) - this package deliberately
/// ships no default implementation, so there is no hardcoded-credential footgun to accidentally
/// deploy.
/// </summary>
public interface IBasicAuthCredentialValidator
{
    /// <summary>
    /// Validates a username/password pair.
    /// </summary>
    /// <param name="username">The username portion of the decoded credentials (everything before the first <c>:</c>).</param>
    /// <param name="password">The password portion of the decoded credentials (everything after the first <c>:</c> - may itself contain <c>:</c> characters, per RFC 7617).</param>
    /// <returns>
    /// The authenticated principal on success, or <c>null</c> on failure. Never throw for "wrong
    /// credentials" - that's an ordinary <c>Unauthorized</c>, not an application error.
    /// </returns>
    Task<ClaimsPrincipal?> ValidateAsync(string username, string password);
}
