using Benzene.Examples.App.Data;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Benzene.Example.Asp.Test.Integration;

// public abstract class OrdersIntegrationTestBase : IntegrationTestBase
// {
//     public Order[] GetPersistedOrders()
//     {
//         return DatabaseSetup.CreateDataContext().Orders.ToArray();
//     }
//
//     public void AddOrder(Order order)
//     {
//         var context = DatabaseSetup.CreateDataContext();
//         context.Add(order);
//         context.SaveChanges();
//     }
// }


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
        // EnvironmentSetUp.SetUp();
        InMemoryOrderDbClient.Orders.Clear();
    }
}

// public abstract class IntegrationTestBase : IDisposable
// {
//     public IntegrationTestBase()
//     {
//         SetUpAsync().Wait();
//     }
//
//     public void Dispose()
//     {
//         TearDownAsync().Wait();
//     }
//
//     private async Task SetUpAsync()
//     {
//         EnvironmentSetUp.SetUp();
//         await Task.WhenAll(
//             SqsSetUp.SetUp(),
//             DatabaseSetup.ResetDatabaseAsync());
//     }
//
//     private async Task TearDownAsync()
//     {
//         await Task.WhenAll(
//             SqsSetUp.TearDown());
//
//     }
// }