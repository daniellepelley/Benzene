using Benzene.Saga;

namespace Benzene.Example.Saga;

/// <summary>
/// The user-signup distributed transaction, expressed as a saga:
///   Stage 1 (parallel): create the tenant AND create the company in Okta.
///   Stage 2:            create the user, using the tenant ID from stage 1.
///   Stage 3:            create the RBAC role, using the user ID from stage 2.
/// Every step carries the compensation that undoes it. If any step fails, the orchestrator rolls
/// the whole thing back in reverse order, leaving no orphaned records - then it can be retried.
/// </summary>
public static class SignupSaga
{
    public static Benzene.Saga.Saga Build(SignupApi api, string companyName)
    {
        return new SagaBuilder()
            .Stage(stage => stage
                .Step<TenantCreated>(step => step
                    .Do(_ => api.CreateTenantAsync(companyName))
                    .Compensate((_, tenant) => api.DeleteTenantAsync(tenant.TenantId)))
                .Step<OktaCompanyCreated>(step => step
                    .Do(_ => api.CreateOktaCompanyAsync(companyName))
                    .Compensate((_, company) => api.DeleteOktaCompanyAsync(company.CompanyId))))
            .Stage(stage => stage
                .Step<UserCreated>(step => step
                    .Do(ctx => api.CreateUserAsync(ctx.Get<TenantCreated>().TenantId))
                    .Compensate((_, user) => api.DeleteUserAsync(user.UserId))))
            .Stage(stage => stage
                .Step<RoleCreated>(step => step
                    .Do(ctx => api.CreateRoleAsync(ctx.Get<UserCreated>().UserId))
                    .Compensate((_, role) => api.DeleteRoleAsync(role.RoleId))))
            .Build();
    }
}
