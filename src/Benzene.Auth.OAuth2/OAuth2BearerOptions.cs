namespace Benzene.Auth.OAuth2;

/// <summary>
/// Configuration for <see cref="Extensions.UseOAuth2Bearer{TContext}"/>. Every field below is
/// deliberately required, with no permissive silent default - see each property's remarks for why.
/// </summary>
public class OAuth2BearerOptions
{
    /// <summary>
    /// The OIDC discovery URL (".../.well-known/openid-configuration"), used to fetch and
    /// auto-refresh the JWKS. Set this OR <see cref="JwksUri"/>, not both - most identity providers
    /// (Auth0, Cognito, Azure AD, Okta) expose full OIDC discovery; <see cref="JwksUri"/> is the
    /// escape hatch for ones that only publish a bare JWKS document.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// A bare JWKS document URL, for identity providers that don't expose full OIDC discovery. Set
    /// this OR <see cref="Authority"/>, not both.
    /// </summary>
    public string? JwksUri { get; set; }

    /// <summary>
    /// Every issuer this service trusts. Required - a token whose <c>iss</c> claim isn't in this
    /// list is rejected before signature validation even runs. No default: an empty list must fail
    /// fast at wire-up (see <see cref="Extensions.UseOAuth2Bearer{TContext}"/>), not silently
    /// accept tokens from any issuer.
    /// </summary>
    public string[] ValidIssuers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Every audience this service accepts. Required for the same reason as
    /// <see cref="ValidIssuers"/> - a token minted for a different service must not be accepted
    /// here (the classic token-confusion mistake).
    /// </summary>
    public string[] ValidAudiences { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Explicit signing-algorithm allowlist (e.g. <c>"RS256"</c> - see
    /// <see cref="Microsoft.IdentityModel.Tokens.SecurityAlgorithms"/> for the standard constants).
    /// Required, no default: a JWT validator that trusts whatever <c>alg</c> the token itself
    /// claims is vulnerable to algorithm-confusion attacks (RFC 8725 §3.1) - this library will not
    /// do that. See this package's <c>CLAUDE.md</c> for the full rationale.
    /// </summary>
    public string[] ValidAlgorithms { get; set; } = Array.Empty<string>();

    /// <summary>Clock skew tolerance applied to <c>exp</c>/<c>nbf</c> validation. Defaults to 2 minutes.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Whether <see cref="Authority"/>/<see cref="JwksUri"/> must be fetched over HTTPS. Defaults
    /// to <c>true</c> - fetching the document that establishes trust (the JWKS) over plain HTTP is
    /// vulnerable to a man-in-the-middle substituting a different signing key, so every real
    /// identity provider serves this over HTTPS and this stays required by default. Set to
    /// <c>false</c> only for local development/testing against a plain-HTTP fake JWKS endpoint -
    /// the same escape hatch ASP.NET Core's own <c>JwtBearerOptions.RequireHttpsMetadata</c>
    /// provides for the identical reason. Never set this <c>false</c> in production.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Validates this instance, throwing <see cref="ArgumentException"/> for any wire-up mistake
    /// that would otherwise silently under-validate every token this middleware sees. Called by
    /// <see cref="Extensions.UseOAuth2Bearer{TContext}"/> at pipeline wire-up time - fail fast,
    /// not on the first request.
    /// </summary>
    internal void Validate()
    {
        var hasAuthority = !string.IsNullOrWhiteSpace(Authority);
        var hasJwksUri = !string.IsNullOrWhiteSpace(JwksUri);

        if (hasAuthority == hasJwksUri)
        {
            throw new ArgumentException(
                $"Exactly one of {nameof(Authority)} or {nameof(JwksUri)} must be set (not both, not neither).",
                hasAuthority ? nameof(JwksUri) : nameof(Authority));
        }

        if (ValidIssuers is not { Length: > 0 })
        {
            throw new ArgumentException(
                $"{nameof(ValidIssuers)} must contain at least one trusted issuer - an empty list would accept tokens from any issuer.",
                nameof(ValidIssuers));
        }

        if (ValidAudiences is not { Length: > 0 })
        {
            throw new ArgumentException(
                $"{nameof(ValidAudiences)} must contain at least one accepted audience - an empty list would accept tokens minted for any audience.",
                nameof(ValidAudiences));
        }

        if (ValidAlgorithms is not { Length: > 0 })
        {
            throw new ArgumentException(
                $"{nameof(ValidAlgorithms)} must contain at least one allowed signing algorithm - " +
                "an empty list would trust whatever \"alg\" the token itself claims (RFC 8725 §3.1 algorithm confusion).",
                nameof(ValidAlgorithms));
        }
    }
}
