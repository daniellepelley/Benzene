using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Benzene.Test.Auth;

/// <summary>
/// A real loopback JWKS endpoint (RFC 7517) backed by locally-generated RSA keys, for exercising
/// <c>Benzene.Auth.OAuth2</c>'s <c>JwksUri</c> path end-to-end without depending on a real identity
/// provider. Serves whatever set of keys is currently registered via <see cref="AddKey"/> - tests
/// that need to simulate key rotation add a second key after the server is already running.
/// </summary>
public sealed class FakeJwksServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly List<(string KeyId, RSA Rsa)> _keys = new();
    private readonly object _gate = new();
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public string JwksUri { get; }

    public FakeJwksServer()
    {
        var port = GetFreeTcpPort();
        // Listen on the bare root prefix and serve the JWKS document for any path - this is a
        // single-purpose fake server, so there's no need to get HttpListener's prefix-matching
        // trailing-slash rules exactly right for a specific path.
        JwksUri = $"http://localhost:{port}/jwks.json";
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _ = Task.Run(RunAsync);
    }

    /// <summary>Generates and registers a new RSA key under the given key id, returning it for token signing.</summary>
    public RSA AddKey(string keyId)
    {
        var rsa = RSA.Create(2048);
        lock (_gate)
        {
            _keys.Add((keyId, rsa));
        }
        return rsa;
    }

    /// <summary>Mints a JWT signed with the given key, with the given issuer/audience/claims/lifetime.</summary>
    public static string CreateToken(
        RSA signingKey, string keyId, string issuer, string audience,
        DateTime? expires = null, DateTime? notBefore = null,
        IDictionary<string, object>? extraClaims = null)
    {
        var key = new RsaSecurityKey(signingKey) { KeyId = keyId };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var claims = new Dictionary<string, object>(extraClaims ?? new Dictionary<string, object>());

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = credentials,
            Claims = claims
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>Mints a JWT signed with an HMAC secret instead of an RSA key - for proving the
    /// algorithm allowlist actually rejects a token whose "alg" isn't on it.</summary>
    public static string CreateHmacSignedToken(string issuer, string audience, string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var json = BuildJwksJson();
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception)
            {
                // Best-effort - the client-side call will observe the failure either way.
            }
        }
    }

    private string BuildJwksJson()
    {
        (string KeyId, RSA Rsa)[] snapshot;
        lock (_gate)
        {
            snapshot = _keys.ToArray();
        }

        var keys = new List<object>();
        foreach (var (keyId, rsa) in snapshot)
        {
            var parameters = rsa.ExportParameters(false);
            keys.Add(new
            {
                kty = "RSA",
                use = "sig",
                kid = keyId,
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus),
                e = Base64UrlEncoder.Encode(parameters.Exponent)
            });
        }

        return JsonSerializer.Serialize(new { keys });
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
        foreach (var (_, rsa) in _keys)
        {
            rsa.Dispose();
        }
    }
}
