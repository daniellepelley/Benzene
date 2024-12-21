using Benzene.Examples.App.Data;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Benzene.Example.Asp.Test.Integration;

public abstract class InMemoryOrdersTestBase
{
    protected HttpClient _client;

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
        var webApplicationFactory = new WebApplicationFactory<Startup>();
        _client = webApplicationFactory.CreateClient();
        InMemoryOrderDbClient.Orders.Clear();
    }
}
