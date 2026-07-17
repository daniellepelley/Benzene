using System.Text;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Auth.Core;
using Benzene.Http;

namespace Benzene.Auth.Basic;

/// <summary>
/// RFC 7617 HTTP Basic authentication middleware: reads <c>Authorization: Basic &lt;base64&gt;</c>,
/// validates the decoded username/password against an <see cref="IBasicAuthCredentialValidator"/>,
/// and either short-circuits with <c>Unauthorized</c> or sets <see cref="AuthenticationHolder.Principal"/>
/// and calls <c>next()</c>.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// Registered scoped (not singleton, unlike e.g. <c>Benzene.Http.Cors.CorsMiddleware</c>) - this
/// middleware's constructor captures <see cref="AuthenticationHolder"/>, which is itself scoped to
/// one message, so the middleware carrying it must be re-resolved per message too. See
/// <c>UseBasicAuth</c>.
/// </remarks>
public class BasicAuthMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private const string SchemePrefix = "Basic ";

    private readonly IBasicAuthCredentialValidator _validator;
    private readonly string _realm;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    private readonly AuthenticationHolder _holder;
    private readonly IServiceResolver _resolver;

    /// <summary>Gets the name of the middleware.</summary>
    public string Name => "BasicAuth";

    /// <summary>Initializes a new instance of the <see cref="BasicAuthMiddleware{TContext}"/> class.</summary>
    /// <param name="validator">Validates the decoded username/password.</param>
    /// <param name="realm">The RFC 7617 realm advertised in the <c>WWW-Authenticate</c> challenge.</param>
    /// <param name="httpRequestAdapter">Adapts the context to a <see cref="HttpRequest"/> to read the <c>Authorization</c> header from.</param>
    /// <param name="responseAdapter">Sets the <c>WWW-Authenticate</c> response header on a challenge.</param>
    /// <param name="holder">The current message's authentication holder, set on success.</param>
    /// <param name="resolver">The current message's service resolver, used to apply Unauthorized results via <see cref="AuthResults"/>.</param>
    public BasicAuthMiddleware(IBasicAuthCredentialValidator validator, string realm,
        IHttpRequestAdapter<TContext> httpRequestAdapter, IBenzeneResponseAdapter<TContext> responseAdapter,
        AuthenticationHolder holder, IServiceResolver resolver)
    {
        _validator = validator;
        _realm = realm;
        _httpRequestAdapter = httpRequestAdapter;
        _responseAdapter = responseAdapter;
        _holder = holder;
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
            await ChallengeAsync(context, "Missing or malformed Authorization header");
            return;
        }

        var encodedCredentials = authorizationHeader.Substring(SchemePrefix.Length).Trim();

        string decodedCredentials;
        try
        {
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            decodedCredentials = Encoding.UTF8.GetString(credentialBytes);
        }
        catch (FormatException)
        {
            await ChallengeAsync(context, "Malformed Basic credentials");
            return;
        }

        // Split on the FIRST ':' only - RFC 7617 explicitly allows ':' inside the password, so a
        // naive Split(':') would truncate or misassign it.
        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex < 0)
        {
            await ChallengeAsync(context, "Malformed Basic credentials");
            return;
        }

        var username = decodedCredentials[..separatorIndex];
        var password = decodedCredentials[(separatorIndex + 1)..];

        var principal = await _validator.ValidateAsync(username, password);
        if (principal is null)
        {
            await ChallengeAsync(context, "Invalid credentials");
            return;
        }

        _holder.Principal = principal;
        await next();
    }

    private async Task ChallengeAsync(TContext context, string detail)
    {
        // Per RFC 7617, a 401 response to a request without valid credentials SHOULD include a
        // WWW-Authenticate challenge - this is what makes browsers/HTTP clients prompt for
        // credentials, so it's set on every Unauthorized outcome this middleware produces, not
        // just the "header missing entirely" case.
        _responseAdapter.SetResponseHeader(context, "WWW-Authenticate", $"Basic realm=\"{_realm}\"");
        await AuthResults.UnauthorizedAsync(_resolver, context, detail);
    }
}
