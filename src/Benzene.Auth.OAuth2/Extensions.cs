using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Auth.Core;
using Benzene.Core.Middleware;
using Benzene.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Benzene.Auth.OAuth2;

/// <summary>
/// Provides extension methods for configuring OAuth2 bearer token (JWT) validation and
/// scope-based authorization in the Benzene pipeline.
/// </summary>
public static class Extensions
{
    private const string LoggerCategory = "Benzene.Auth.OAuth2";

    /// <summary>
    /// Adds OAuth2 bearer token (JWT) validation middleware to the pipeline. Requests without a
    /// valid <c>Authorization: Bearer</c> header, or whose token fails validation (bad signature,
    /// expired, wrong issuer/audience/algorithm), are short-circuited with <c>Unauthorized</c> and
    /// a generic detail message - the real reason is logged server-side only, never returned to the
    /// caller (see this package's <c>CLAUDE.md</c>). Requests that pass have
    /// <see cref="AuthenticationHolder.Principal"/> set for later pipeline steps (including
    /// <see cref="RequireScope{TContext}"/>) and continue to <c>next()</c>.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="options">
    /// The validation configuration - see <see cref="OAuth2BearerOptions"/>. Validated at wire-up
    /// time (<see cref="OAuth2BearerOptions.Validate"/>): <see cref="ArgumentException"/> is thrown
    /// immediately, not on the first request, if <see cref="OAuth2BearerOptions.Authority"/>/
    /// <see cref="OAuth2BearerOptions.JwksUri"/> aren't set exactly one at a time, or if
    /// <see cref="OAuth2BearerOptions.ValidIssuers"/>/<see cref="OAuth2BearerOptions.ValidAudiences"/>/
    /// <see cref="OAuth2BearerOptions.ValidAlgorithms"/> are empty.
    /// </param>
    /// <returns>The middleware pipeline builder for method chaining.</returns>
    /// <remarks>
    /// Registers <see cref="AuthenticationHolder"/> scoped in this extension (not centrally) - the
    /// Context Purity pattern (see <c>Benzene.Auth.Core/CLAUDE.md</c>): a pipeline that never calls
    /// <c>UseOAuth2Bearer</c>/<c>UseBasicAuth</c> never allocates a holder anyone would look at. The
    /// <see cref="JsonWebTokenHandler"/>, <see cref="TokenValidationParameters"/>, and its
    /// JWKS-caching <c>ConfigurationManager</c> are built once here, at wire-up time, and shared
    /// across every request - not rebuilt per message.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseOAuth2Bearer<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, OAuth2BearerOptions options)
        where TContext : IHttpContext
    {
        options.Validate();

        var configurationManager = OAuth2ConfigurationManagerFactory.Create(options);
        var tokenHandler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuers = options.ValidIssuers,
            ValidAudiences = options.ValidAudiences,
            ValidAlgorithms = options.ValidAlgorithms,
            ClockSkew = options.ClockSkew,
            ConfigurationManager = configurationManager,

            // Explicit, not left to library defaults - every one of these must genuinely run for
            // ValidIssuers/ValidAudiences/ValidAlgorithms above to mean anything.
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };

        app.Register(x =>
        {
            x.TryAddScoped<AuthenticationHolder>();
            x.AddScoped(resolver => new OAuth2BearerMiddleware<TContext>(
                tokenHandler, validationParameters,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<AuthenticationHolder>(),
                resolver.GetService<ILoggerFactory>().CreateLogger(LoggerCategory),
                resolver
            ));
        });

        return app.Use<TContext, OAuth2BearerMiddleware<TContext>>();
    }

    /// <summary>
    /// Adds authorization middleware that requires the current message's authenticated caller (set
    /// by <see cref="UseOAuth2Bearer{TContext}"/>, earlier in the pipeline) to hold at least one of
    /// <paramref name="anyOfScopes"/>, read from either the <c>scope</c> claim (RFC 8693,
    /// space-delimited) or the <c>scp</c> claim (Azure AD's convention - a space-delimited string
    /// OR a JSON array, depending on issuer; both are normalized - see <see cref="ScopeClaims"/>).
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="anyOfScopes">The set of scopes the caller may hold - any one is sufficient.</param>
    /// <returns>The middleware pipeline builder for method chaining.</returns>
    /// <remarks>
    /// No principal at all (no authentication middleware ran, or the one that ran failed) yields
    /// <c>Unauthorized</c> - not <c>Forbidden</c>. A principal missing every requested scope yields
    /// <c>Forbidden</c>. Collapsing these would be a real information-loss bug for API consumers
    /// debugging a 403 they can't explain (wire-contracts.md §3). Lives in <c>Benzene.Auth.OAuth2</c>
    /// rather than <c>Benzene.Auth.Core</c> because scopes are specifically an OAuth2/JWT concept,
    /// not a mechanism-agnostic one (design doc §8 Q3).
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> RequireScope<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params string[] anyOfScopes)
        where TContext : IHttpContext
    {
        app.Register(x => x.TryAddScoped<AuthenticationHolder>());

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("RequireScope", async (context, next) =>
        {
            var holder = resolver.GetService<AuthenticationHolder>();
            if (holder.Principal is null)
            {
                await AuthResults.UnauthorizedAsync(resolver, context, "No authenticated caller");
                return;
            }

            var granted = ScopeClaims.GetGrantedScopes(holder.Principal);
            if (!anyOfScopes.Any(granted.Contains))
            {
                await AuthResults.ForbiddenAsync(resolver, context,
                    $"Missing required scope (any of: {string.Join(", ", anyOfScopes)})");
                return;
            }

            await next();
        }));
    }
}
