using Benzene.Examples.App.Data;
using Benzene.Examples.Kafka.Test.Helpers;

namespace Benzene.Examples.Kafka.Test.Integration;

public abstract class InMemoryOrdersTestBase : IDisposable
{
    protected InMemoryOrdersTestBase()
    {
        SetUp();
    }

    public Order[] GetPersistedOrders()
    {
        return InMemoryOrderDbClient.Orders.ToArray();
    }

    public void AddOrder(Order order)
    {
        InMemoryOrderDbClient.Orders.Add(order);
    }

    private void SetUp()
    {
        // EnvironmentSetUp.SetUp();
        KafkaSetUp.SetUpAsync().Wait();
        WorkerSetUp.SetUp();
        InMemoryOrderDbClient.Orders.Clear();
    }

    public void Dispose()
    {
        KafkaSetUp.TearDownAsync().Wait();
        WorkerSetUp.TearDownAsync().Wait();
    }
}