using System.Net;
using Xunit;

namespace Benzene.Example.Asp.Test.Integration;

/// <summary>
/// Covers the Asp example's authorization surface - the <c>/protected</c> route wired in
/// <c>Startup.Configure</c> with <c>UseOAuth2Bearer(...)</c> + <c>RequireScope("orders:read")</c>,
/// isolated behind <c>app.Map("/protected", ...)</c>. This asserts the load-bearing half of that
/// wiring in-memory: an <b>anonymous</b> caller is rejected with <c>401 Unauthorized</c>, never
/// reaching <c>ProtectedPingMessageHandler</c>.
///
/// This is the regression guard that matters most for the example: if the <c>app.Map</c> isolation
/// or the bearer middleware were ever broken, the protected handler would become publicly reachable
/// (a silent 200 for an anonymous caller) - exactly what this test would catch. The full happy-path
/// (a validly-scoped token is admitted, a wrong-scope token gets 403) depends on the demo identity
/// provider's JWKS being fetched over a real loopback port (<c>DemoJwtIssuer.Issuer</c> is a fixed
/// <c>http://localhost:5000/</c>), which the in-memory <c>WebApplicationFactory</c> TestServer does
/// not bind - that positive path is already proven at the library level in
/// <c>test/Benzene.Core.Test/Auth/OAuth2BearerTest.cs</c> against a real loopback JWKS server.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Sequential")]
public class ProtectedRouteTest : InMemoryOrdersTestBase
{
    [Fact]
    public async Task ProtectedPing_WithoutAToken_IsRejectedUnauthorized()
    {
        // No Authorization header at all - the bearer middleware short-circuits before any JWKS fetch.
        var response = await _client.GetAsync("/protected/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedPing_WithAGarbageToken_IsRejectedUnauthorized()
    {
        // A malformed bearer token fails validation (and never leaks why - see the package's CLAUDE.md);
        // either way an unauthenticated caller must not reach the protected handler.
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-jwt");
        var response = await _client.GetAsync("/protected/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
