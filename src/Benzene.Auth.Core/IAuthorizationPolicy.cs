using System.Security.Claims;

namespace Benzene.Auth.Core;

/// <summary>
/// A named authorization rule evaluated against the authenticated caller. Benzene owns the
/// <em>enforcement</em> mechanism (<c>RequirePolicy</c> short-circuits with <c>Forbidden</c> when a
/// policy isn't satisfied); an application implements this interface to define what a policy
/// actually <em>means</em>, keeping domain rules out of the framework.
/// </summary>
/// <remarks>
/// Principal-based by design: a rule that needs the specific resource being acted on (e.g. "the
/// caller owns this order") is a resource decision — use <see cref="IAuthorizationHandler{TResource}"/>
/// with <c>RequireAuthorization</c> for that. Register policies with
/// <c>AuthorizationExtensions.AddAuthorizationPolicy</c> to reference them by name.
/// </remarks>
public interface IAuthorizationPolicy
{
    /// <summary>The policy's name, used to reference it from <c>RequirePolicy(name)</c>.</summary>
    string Name { get; }

    /// <summary>Returns whether the authenticated caller satisfies this policy.</summary>
    /// <param name="principal">The authenticated caller for the current message.</param>
    Task<bool> IsSatisfiedAsync(ClaimsPrincipal principal);
}
