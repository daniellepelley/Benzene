using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Auth.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Example.Asp.DemoAuth;

public class ProtectedPingResponse
{
    public string Message { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// A route protected by <c>UseOAuth2Bearer</c> + <c>RequireScope("orders:read")</c> (see
/// <see cref="Startup"/>'s second <c>UseHttp</c> pipeline) - proves authentication actually ran by
/// echoing back the scopes on the caller's validated token, read from
/// <see cref="AuthenticationHolder"/>. See docs/cookbooks/auth-patterns.md.
/// </summary>
// Registered inside an app.Map("/protected", ...) branch (see Startup.cs) - ASP.NET Core strips
// the matched "/protected" prefix from the request path for everything inside that branch, so the
// route Benzene sees here is "/ping", not "/protected/ping".
[HttpEndpoint("GET", "/ping")]
[Message("protected:ping")]
public class ProtectedPingMessageHandler : IMessageHandler<Void, ProtectedPingResponse>
{
    private readonly AuthenticationHolder _authenticationHolder;

    public ProtectedPingMessageHandler(AuthenticationHolder authenticationHolder)
    {
        _authenticationHolder = authenticationHolder;
    }

    public Task<IBenzeneResult<ProtectedPingResponse>> HandleAsync(Void request)
    {
        var scopeClaims = _authenticationHolder.Principal?.FindAll("scope").Select(c => c.Value) ?? Enumerable.Empty<string>();
        var response = new ProtectedPingResponse
        {
            Message = "You are authenticated - RequireScope(\"orders:read\") let this request through.",
            Scopes = scopeClaims.SelectMany(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray()
        };
        return Task.FromResult(BenzeneResult.Ok(response));
    }
}
