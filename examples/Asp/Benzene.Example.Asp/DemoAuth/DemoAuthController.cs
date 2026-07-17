using Microsoft.AspNetCore.Mvc;

namespace Benzene.Example.Asp.DemoAuth;

/// <summary>
/// Serves the demo identity provider's JWKS document and mints demo tokens - see
/// <see cref="DemoJwtIssuer"/> and docs/cookbooks/auth-patterns.md. Demo-only; a real service has
/// no equivalent of this controller, since a real identity provider serves these.
/// </summary>
[ApiController]
public class DemoAuthController : ControllerBase
{
    private readonly DemoJwtIssuer _issuer;

    public DemoAuthController(DemoJwtIssuer issuer)
    {
        _issuer = issuer;
    }

    [HttpGet(".well-known/jwks.json")]
    public ContentResult Jwks()
    {
        return Content(_issuer.JwksJson(), "application/json");
    }

    /// <summary>
    /// Mints a demo bearer token with the given scope(s), e.g. <c>/demo-token?scope=orders:read</c>.
    /// Copy the returned token into an <c>Authorization: Bearer &lt;token&gt;</c> header to call
    /// <c>GET /protected/ping</c>.
    /// </summary>
    [HttpGet("demo-token")]
    public IActionResult Token([FromQuery] string scope = "orders:read")
    {
        return Content(_issuer.IssueToken(scope), "text/plain");
    }
}
