using System.Security.Claims;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Auth.Core;
using Benzene.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Benzene.Auth.OAuth2;

/// <summary>
/// OAuth2 bearer token (JWT) validation middleware: reads <c>Authorization: Bearer &lt;token&gt;</c>,
/// validates it via <see cref="JsonWebTokenHandler"/> against the pre-built
/// <see cref="TokenValidationParameters"/> (signature, issuer, audience, algorithm, lifetime - see
/// <see cref="Extensions.UseOAuth2Bearer{TContext}"/>), and either short-circuits with
/// <c>Unauthorized</c> or sets <see cref="AuthenticationHolder.Principal"/> and calls <c>next()</c>.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// Registered scoped (not singleton) - like <c>Benzene.Auth.Basic.BasicAuthMiddleware{TContext}</c>,
/// this middleware's constructor captures <see cref="AuthenticationHolder"/>, which is itself
/// scoped to one message, so the middleware carrying it must be re-resolved per message too. The
/// expensive, genuinely-reusable pieces (<see cref="JsonWebTokenHandler"/>, the shared
/// <see cref="TokenValidationParameters"/>, and its JWKS-caching <c>ConfigurationManager</c>) are
/// built once at wire-up time by <see cref="Extensions.UseOAuth2Bearer{TContext}"/> and passed in,
/// not rebuilt per message.
/// </remarks>
public class OAuth2BearerMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private const string SchemePrefix = "Bearer ";

    private readonly JsonWebTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly AuthenticationHolder _holder;
    private readonly ILogger _logger;
    private readonly IServiceResolver _resolver;

    /// <summary>Gets the name of the middleware.</summary>
    public string Name => "OAuth2Bearer";

    /// <summary>Initializes a new instance of the <see cref="OAuth2BearerMiddleware{TContext}"/> class.</summary>
    /// <param name="tokenHandler">The shared JWT handler used to validate incoming bearer tokens.</param>
    /// <param name="validationParameters">The shared, pre-built validation parameters (issuer/audience/algorithm allowlists, JWKS resolution).</param>
    /// <param name="httpRequestAdapter">Adapts the context to a <see cref="HttpRequest"/> to read the <c>Authorization</c> header from.</param>
    /// <param name="holder">The current message's authentication holder, set on success.</param>
    /// <param name="logger">Used to log the real reason a token failed validation - never returned to the caller.</param>
    /// <param name="resolver">The current message's service resolver, used to apply Unauthorized results via <see cref="AuthResults"/>.</param>
    public OAuth2BearerMiddleware(JsonWebTokenHandler tokenHandler, TokenValidationParameters validationParameters,
        IHttpRequestAdapter<TContext> httpRequestAdapter, AuthenticationHolder holder, ILogger logger, IServiceResolver resolver)
    {
        _tokenHandler = tokenHandler;
        _validationParameters = validationParameters;
        _httpRequestAdapter = httpRequestAdapter;
        _holder = holder;
        _logger = logger;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var httpRequest = _httpRequestAdapter.Map(context).AsLowerCase();

        if (!httpRequest.Headers.TryGetValue("authorization", out var authorizationHeader) ||
            string.IsNullOrEmpty(authorizationHeader) ||
            !authorizationHeader.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            await AuthResults.UnauthorizedAsync(_resolver, context, "Missing bearer token");
            return;
        }

        var token = authorizationHeader.Substring(SchemePrefix.Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            await AuthResults.UnauthorizedAsync(_resolver, context, "Missing bearer token");
            return;
        }

        TokenValidationResult result;
        try
        {
            result = await _tokenHandler.ValidateTokenAsync(token, _validationParameters);
        }
        catch (Exception ex)
        {
            // ValidateTokenAsync is documented to report failures via TokenValidationResult rather
            // than throwing, but an unreachable JWKS endpoint or similar infrastructure failure can
            // still surface as an exception - treat it the same as an ordinary validation failure
            // rather than letting it propagate as an unhandled exception.
            _logger.LogError(ex, "OAuth2 bearer token validation threw unexpectedly");
            await AuthResults.UnauthorizedAsync(_resolver, context, "Invalid bearer token");
            return;
        }

        if (!result.IsValid)
        {
            // Never echo the real validation failure reason back to the caller (a bad signature vs.
            // an expired token vs. a wrong audience is an oracle for an attacker probing token
            // shapes) - log it server-side only, per the design doc §3.3/§6.
            _logger.LogInformation(result.Exception, "OAuth2 bearer token failed validation");
            await AuthResults.UnauthorizedAsync(_resolver, context, "Invalid bearer token");
            return;
        }

        _holder.Principal = new ClaimsPrincipal(result.ClaimsIdentity);
        await next();
    }
}
