using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Example.Saga;

// Stand-in for the downstream services a signup calls. In a real Benzene system each of these
// methods is a call to another service via IBenzeneMessageSender:
//
//     _ => sender.SendAsync<CreateTenant, TenantCreated>("tenant:create", new CreateTenant(...))
//
// which already returns Task<IBenzeneResult<T>> - exactly what a saga step's Do(...) wants, so no
// adapter is needed. Here we back them with an in-memory store so the example runs on its own and
// you can see the effect of rollback.
public record CreateTenant(string CompanyName);
public record TenantCreated(string TenantId);
public record OktaCompanyCreated(string CompanyId);
public record UserCreated(string UserId);
public record RoleCreated(string RoleId);

public class SignupApi
{
    private readonly Store _store;
    private int _counter;

    /// <summary>Set to a record kind (e.g. "user") to force that create to fail, to demonstrate rollback.</summary>
    public string? FailAt { get; set; }

    public SignupApi(Store store)
    {
        _store = store;
    }

    public Task<IBenzeneResult<TenantCreated>> CreateTenantAsync(string companyName)
        => Create("tenant", id => new TenantCreated(id));

    public Task<IBenzeneResult<OktaCompanyCreated>> CreateOktaCompanyAsync(string companyName)
        => Create("okta-company", id => new OktaCompanyCreated(id));

    public Task<IBenzeneResult<UserCreated>> CreateUserAsync(string tenantId)
        => Create("user", id => new UserCreated(id));

    public Task<IBenzeneResult<RoleCreated>> CreateRoleAsync(string userId)
        => Create("rbac-role", id => new RoleCreated(id));

    public Task<IBenzeneResult> DeleteTenantAsync(string id) => Delete("tenant", id);
    public Task<IBenzeneResult> DeleteOktaCompanyAsync(string id) => Delete("okta-company", id);
    public Task<IBenzeneResult> DeleteUserAsync(string id) => Delete("user", id);
    public Task<IBenzeneResult> DeleteRoleAsync(string id) => Delete("rbac-role", id);

    private Task<IBenzeneResult<T>> Create<T>(string kind, Func<string, T> map)
    {
        if (FailAt == kind)
        {
            Console.WriteLine($"  ✗ create {kind} FAILED");
            return Task.FromResult(BenzeneResult.ServiceUnavailable<T>());
        }

        var id = $"{kind}-{Interlocked.Increment(ref _counter)}";
        _store.Add(kind, id);
        Console.WriteLine($"  ✓ created {kind} {id}");
        return Task.FromResult(BenzeneResult.Created(map(id)));
    }

    private Task<IBenzeneResult> Delete(string kind, string id)
    {
        _store.Remove(kind, id);
        Console.WriteLine($"  ↩ compensated: deleted {kind} {id}");
        return Task.FromResult(BenzeneResult.Deleted());
    }
}
