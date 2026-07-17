using System.Security.Claims;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Auth.Core;

/// <summary>
/// Mechanism-agnostic authorization middleware that builds on the <see cref="AuthenticationHolder"/>
/// principal set by an authentication middleware (<c>UseOAuth2Bearer</c>, <c>UseBasicAuth</c>, or any
/// mechanism that sets a <see cref="ClaimsPrincipal"/>). This is the RBAC/policy layer the auth
/// design deliberately left as an app concern layered on top of the principal — roles, named
/// policies, and resource-based checks — expressed as pipeline middleware, the same shape as
/// <c>Benzene.Auth.OAuth2</c>'s <c>RequireScope</c>.
/// </summary>
/// <remarks>
/// Lives in <see cref="Benzene.Auth.Core"/> (not a mechanism-specific package) because roles,
/// policies, and resource checks are all read off a plain <see cref="ClaimsPrincipal"/> — unlike
/// OAuth2 scopes, they aren't tied to JWTs. Every check reports <c>Unauthorized</c> when no caller is
/// authenticated and <c>Forbidden</c> when an authenticated caller lacks permission, preserving the
/// wire-contracts.md §3 distinction. Not constrained to <c>IHttpContext</c>: authorization only reads
/// the scoped principal, so it composes on any transport whose pipeline sets one.
/// </remarks>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Requires the authenticated caller to hold at least one of <paramref name="anyOfRoles"/>. Roles
    /// are read from the caller's <see cref="ClaimsPrincipal.IsInRole"/> and the common role claim
    /// types (<see cref="ClaimTypes.Role"/>, <c>role</c>, <c>roles</c> — the last also accepted as a
    /// JSON array, Azure AD's app-roles shape).
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="anyOfRoles">The roles the caller may hold — any one is sufficient.</param>
    /// <remarks>
    /// No principal (no authentication middleware ran, or it failed) yields <c>Unauthorized</c>; an
    /// authenticated caller missing every role yields <c>Forbidden</c>.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> RequireRole<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params string[] anyOfRoles)
    {
        app.Register(x => x.TryAddScoped<AuthenticationHolder>());

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("RequireRole", async (context, next) =>
        {
            var holder = resolver.GetService<AuthenticationHolder>();
            if (holder.Principal is null)
            {
                await AuthResults.UnauthorizedAsync(resolver, context, "No authenticated caller");
                return;
            }

            if (!RoleClaims.IsInAnyRole(holder.Principal, anyOfRoles))
            {
                await AuthResults.ForbiddenAsync(resolver, context,
                    $"Missing required role (any of: {string.Join(", ", anyOfRoles)})");
                return;
            }

            await next();
        }));
    }

    /// <summary>
    /// Requires the authenticated caller to satisfy <paramref name="policy"/>.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="policy">The policy to evaluate.</param>
    public static IMiddlewarePipelineBuilder<TContext> RequirePolicy<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, IAuthorizationPolicy policy)
    {
        app.Register(x => x.TryAddScoped<AuthenticationHolder>());
        return app.Use(resolver => PolicyMiddleware<TContext>(resolver, policy));
    }

    /// <summary>
    /// Requires the authenticated caller to satisfy the registered policy named
    /// <paramref name="policyName"/>. Resolves an <see cref="IAuthorizationPolicy"/> with that name
    /// from DI (register it with <see cref="AddAuthorizationPolicy(IBenzeneServiceContainer, IAuthorizationPolicy)"/>).
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="policyName">The name of the registered policy to enforce.</param>
    public static IMiddlewarePipelineBuilder<TContext> RequirePolicy<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string policyName)
    {
        app.Register(x => x.TryAddScoped<AuthenticationHolder>());
        return app.Use(resolver =>
        {
            var policy = resolver.GetServices<IAuthorizationPolicy>().FirstOrDefault(p => p.Name == policyName)
                ?? throw new InvalidOperationException(
                    $"No IAuthorizationPolicy named '{policyName}' is registered. Register one with AddAuthorizationPolicy.");
            return PolicyMiddleware<TContext>(resolver, policy);
        });
    }

    /// <summary>
    /// Requires the authenticated caller to satisfy an inline predicate policy.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="policyName">A name for the policy, used in the <c>Forbidden</c> detail message.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public static IMiddlewarePipelineBuilder<TContext> RequirePolicy<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string policyName, Func<ClaimsPrincipal, Task<bool>> predicate)
        => app.RequirePolicy(new DelegateAuthorizationPolicy(policyName, predicate));

    /// <summary>
    /// Requires the authenticated caller to satisfy an inline synchronous predicate policy.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="policyName">A name for the policy, used in the <c>Forbidden</c> detail message.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public static IMiddlewarePipelineBuilder<TContext> RequirePolicy<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string policyName, Func<ClaimsPrincipal, bool> predicate)
        => app.RequirePolicy(new DelegateAuthorizationPolicy(policyName, predicate));

    /// <summary>
    /// Requires the authenticated caller to be authorized for a resource derived from the current
    /// message, via a registered <see cref="IAuthorizationHandler{TResource}"/>.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <typeparam name="TResource">The resource type the decision is made against.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="resourceSelector">Maps the message context to the resource being acted on.</param>
    public static IMiddlewarePipelineBuilder<TContext> RequireAuthorization<TContext, TResource>(
        this IMiddlewarePipelineBuilder<TContext> app, Func<TContext, TResource> resourceSelector)
    {
        app.Register(x => x.TryAddScoped<AuthenticationHolder>());

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("RequireAuthorization", async (context, next) =>
        {
            var holder = resolver.GetService<AuthenticationHolder>();
            if (holder.Principal is null)
            {
                await AuthResults.UnauthorizedAsync(resolver, context, "No authenticated caller");
                return;
            }

            var handler = resolver.GetService<IAuthorizationHandler<TResource>>();
            var resource = resourceSelector(context);
            if (!await handler.IsAuthorizedAsync(holder.Principal, resource))
            {
                await AuthResults.ForbiddenAsync(resolver, context, "Not authorized for the requested resource");
                return;
            }

            await next();
        }));
    }

    /// <summary>Registers an <see cref="IAuthorizationPolicy"/> so it can be referenced by name.</summary>
    /// <param name="services">The service container.</param>
    /// <param name="policy">The policy to register.</param>
    public static IBenzeneServiceContainer AddAuthorizationPolicy(
        this IBenzeneServiceContainer services, IAuthorizationPolicy policy)
    {
        services.AddSingleton(policy);
        return services;
    }

    /// <summary>Registers an inline predicate policy so it can be referenced by name.</summary>
    /// <param name="services">The service container.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public static IBenzeneServiceContainer AddAuthorizationPolicy(
        this IBenzeneServiceContainer services, string name, Func<ClaimsPrincipal, Task<bool>> predicate)
        => services.AddAuthorizationPolicy(new DelegateAuthorizationPolicy(name, predicate));

    /// <summary>Registers an inline synchronous predicate policy so it can be referenced by name.</summary>
    /// <param name="services">The service container.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public static IBenzeneServiceContainer AddAuthorizationPolicy(
        this IBenzeneServiceContainer services, string name, Func<ClaimsPrincipal, bool> predicate)
        => services.AddAuthorizationPolicy(new DelegateAuthorizationPolicy(name, predicate));

    private static FuncWrapperMiddleware<TContext> PolicyMiddleware<TContext>(
        IServiceResolver resolver, IAuthorizationPolicy policy)
        => new("RequirePolicy", async (context, next) =>
        {
            var holder = resolver.GetService<AuthenticationHolder>();
            if (holder.Principal is null)
            {
                await AuthResults.UnauthorizedAsync(resolver, context, "No authenticated caller");
                return;
            }

            if (!await policy.IsSatisfiedAsync(holder.Principal))
            {
                await AuthResults.ForbiddenAsync(resolver, context, $"Policy '{policy.Name}' not satisfied");
                return;
            }

            await next();
        });
}
