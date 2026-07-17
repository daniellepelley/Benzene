using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Benzene.Auth.OAuth2;

/// <summary>
/// An <c>IConfigurationRetriever&lt;OpenIdConnectConfiguration&gt;</c> for identity providers that
/// only publish a bare JWKS document (<see cref="OAuth2BearerOptions.JwksUri"/>), not full OIDC
/// discovery. Fetches the JWKS document directly and wraps it in a minimal
/// <see cref="OpenIdConnectConfiguration"/> carrying just the signing keys - everything
/// <c>JsonWebTokenHandler</c> needs to resolve a token's signing key via
/// <c>TokenValidationParameters.ConfigurationManager</c>.
/// </summary>
/// <remarks>
/// Used with <c>ConfigurationManager&lt;OpenIdConnectConfiguration&gt;</c> the same way
/// <see cref="OpenIdConnectConfigurationRetriever"/> is for the <see cref="OAuth2BearerOptions.Authority"/>
/// path - so both paths get the same caching/refresh-on-unrecognized-<c>kid</c> behavior "for free"
/// from <c>ConfigurationManager</c>, without this middleware reimplementing it.
/// </remarks>
internal sealed class JwksOnlyConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    /// <inheritdoc />
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
        var keySet = new JsonWebKeySet(json);

        var configuration = new OpenIdConnectConfiguration { JsonWebKeySet = keySet };
        foreach (var key in keySet.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}
