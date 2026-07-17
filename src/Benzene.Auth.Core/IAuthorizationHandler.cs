using System.Security.Claims;

namespace Benzene.Auth.Core;

/// <summary>
/// A resource-based authorization hook: decides whether the authenticated caller may act on a
/// specific resource. Register an implementation in DI and enforce it with
/// <c>RequireAuthorization&lt;TContext, TResource&gt;</c>, which maps the current message's context
/// to a <typeparamref name="TResource"/> and calls this handler. Benzene owns the enforcement; the
/// application owns what "authorized for this resource" means.
/// </summary>
/// <remarks>
/// The resource is derived from the transport context (topic, headers, body, or a route/query
/// value) because authorization runs before the pipeline maps the request into a typed handler
/// argument. Where a decision needs the fully-deserialized request, do the check inside the handler
/// against the same <see cref="AuthenticationHolder"/> principal instead.
/// </remarks>
/// <typeparam name="TResource">The resource type the decision is made against.</typeparam>
public interface IAuthorizationHandler<in TResource>
{
    /// <summary>Returns whether the caller is authorized for the given resource.</summary>
    /// <param name="principal">The authenticated caller for the current message.</param>
    /// <param name="resource">The resource the caller is trying to act on.</param>
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, TResource resource);
}
