using Benzene.Examples.App.Data;
using Benzene.Examples.Google.Tests.Helpers;

namespace Benzene.Examples.Google.Tests.Integration;

public abstract class InMemoryOrdersTestBase
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
        EnvironmentSetUp.SetUp();
        InMemoryOrderDbClient.Orders.Clear();
    }
}