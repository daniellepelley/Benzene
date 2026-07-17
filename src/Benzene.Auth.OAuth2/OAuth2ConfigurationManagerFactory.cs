using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Benzene.Auth.OAuth2;

/// <summary>
/// Builds the single, long-lived <c>ConfigurationManager&lt;OpenIdConnectConfiguration&gt;</c>
/// used by <see cref="Extensions.UseOAuth2Bearer{TContext}"/> - constructed once at pipeline
/// wire-up time (not per request/per message), so its JWKS caching/refresh-on-unrecognized-<c>kid</c>
/// behavior actually caches across requests instead of hitting the identity provider every time.
/// </summary>
internal static class OAuth2ConfigurationManagerFactory
{
    /// <summary>
    /// Creates a <c>ConfigurationManager&lt;OpenIdConnectConfiguration&gt;</c> for the given,
    /// already-<see cref="OAuth2BearerOptions.Validate"/>-d options: full OIDC discovery
    /// (<see cref="OpenIdConnectConfigurationRetriever"/>) when <see cref="OAuth2BearerOptions.Authority"/>
    /// is set, or a bare-JWKS <see cref="JwksOnlyConfigurationRetriever"/> when
    /// <see cref="OAuth2BearerOptions.JwksUri"/> is set instead.
    /// </summary>
    public static ConfigurationManager<OpenIdConnectConfiguration> Create(OAuth2BearerOptions options)
    {
        var documentRetriever = new HttpDocumentRetriever();

        return !string.IsNullOrWhiteSpace(options.Authority)
            ? new ConfigurationManager<OpenIdConnectConfiguration>(options.Authority, new OpenIdConnectConfigurationRetriever(), documentRetriever)
            : new ConfigurationManager<OpenIdConnectConfiguration>(options.JwksUri!, new JwksOnlyConfigurationRetriever(), documentRetriever);
    }
}
