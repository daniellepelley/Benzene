# Benzene saga example — distributed transaction with rollback

A runnable console example of [`Benzene.Saga`](../../../src/Benzene.Saga): a user-signup flow that
spans several services and either **completes in full or rolls back in full**, leaving no orphaned
records — so it can be safely retried.

The saga (see `SignupSaga.cs`):

- **Stage 1** (parallel): create the tenant **and** create the company in Okta.
- **Stage 2**: create the user, using the tenant ID from stage 1.
- **Stage 3**: create the RBAC role, using the user ID from stage 2.

Each step carries the compensation that undoes it. If any step fails, the orchestrator compensates
every completed effect in reverse order.

In a real Benzene system each step's `Do(...)` is a call to another service via
`IBenzeneMessageSender.SendAsync("tenant:create", req)` (which already returns
`Task<IBenzeneResult<T>>`, so no adapter is needed). Here the calls are backed by an in-memory store
(`SignupApi`/`Store`) so the example runs on its own and you can see rollback take effect.

## Run it

```bash
dotnet run --project examples/Saga/Benzene.Example.Saga
```

Expected output:

```
1) Happy path - everything succeeds
  ✓ created tenant tenant-1
  ✓ created okta-company okta-company-2
  ✓ created user user-3
  ✓ created rbac-role rbac-role-4
  outcome: Succeeded
  store: tenant:tenant-1, okta-company:okta-company-2, user:user-3, rbac-role:rbac-role-4

2) Stage 3 fails - the whole saga rolls back
  ✓ created tenant tenant-1
  ✓ created okta-company okta-company-2
  ✓ created user user-3
  ✗ create rbac-role FAILED
  ↩ compensated: deleted user user-3
  ↩ compensated: deleted tenant tenant-1
  ↩ compensated: deleted okta-company okta-company-2
  outcome: RolledBack (failed at stage 3)
  store: empty - no orphaned records
```

The second run shows the point: the RBAC step fails, and the saga rolls back the user (stage 2) then
the tenant and Okta company (stage 1) in reverse order, leaving the store empty.
