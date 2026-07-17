using Benzene.Example.Saga;
using Benzene.Saga;

// Demonstrates Benzene.Saga: a distributed transaction that either completes in full or rolls back
// in full, leaving no orphaned records - so it can be safely retried.

await RunAsync("1) Happy path - everything succeeds", failAt: null);
await RunAsync("2) Stage 3 fails - the whole saga rolls back", failAt: "rbac-role");

return;

static async Task RunAsync(string title, string? failAt)
{
    Console.WriteLine();
    Console.WriteLine(title);

    var store = new Store();
    var api = new SignupApi(store) { FailAt = failAt };

    var saga = SignupSaga.Build(api, companyName: "Acme Ltd");
    SagaResult result = await saga.RunAsync();

    Console.WriteLine($"  outcome: {result.Outcome}" +
        (result.FailedStageIndex is { } i ? $" (failed at stage {i + 1})" : ""));

    var remaining = store.Snapshot();
    Console.WriteLine(remaining.Count == 0
        ? "  store: empty - no orphaned records"
        : $"  store: {string.Join(", ", remaining)}");
}
