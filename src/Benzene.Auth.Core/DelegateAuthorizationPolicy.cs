using System.Security.Claims;

namespace Benzene.Auth.Core;

/// <summary>
/// An <see cref="IAuthorizationPolicy"/> backed by an inline predicate, so a simple policy can be
/// defined without a dedicated class. Used by the <c>RequirePolicy(name, predicate)</c> and
/// <c>AddAuthorizationPolicy(name, predicate)</c> convenience overloads.
/// </summary>
public class DelegateAuthorizationPolicy : IAuthorizationPolicy
{
    private readonly Func<ClaimsPrincipal, Task<bool>> _predicate;

    /// <summary>Initializes a policy from an async predicate.</summary>
    /// <param name="name">The policy's name.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public DelegateAuthorizationPolicy(string name, Func<ClaimsPrincipal, Task<bool>> predicate)
    {
        Name = name;
        _predicate = predicate;
    }

    /// <summary>Initializes a policy from a synchronous predicate.</summary>
    /// <param name="name">The policy's name.</param>
    /// <param name="predicate">Evaluates whether the caller satisfies the policy.</param>
    public DelegateAuthorizationPolicy(string name, Func<ClaimsPrincipal, bool> predicate)
        : this(name, principal => Task.FromResult(predicate(principal)))
    {
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<bool> IsSatisfiedAsync(ClaimsPrincipal principal) => _predicate(principal);
}
