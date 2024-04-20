using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Google.Tests.Helpers;
using Benzene.Examples.Google.Tests.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Google.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class CreateOrderTest : InMemoryOrdersTestBase
{
    private CreateOrderMessage CreateCreateOrderMessage()
    {
        return new CreateOrderMessage { Status = Defaults.Order.Status, Name = Defaults.Order.Name };
    }

    [Fact]
    public async Task CreateOrder_Http()
    {
        var httpContext = new HttpContextBuilder("POST", "/orders")
            .WithBody(CreateCreateOrderMessage())
            .Build();

        await TestFunctionHosting.SendHttpContextAsync(httpContext);
        var response = httpContext.Response;

        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        var order = httpContext.Response.Body<string>();

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(order);
    }


    [Fact]
    public async Task CreateOrder_ValidationFailure()
    {
        var httpContext = new HttpContextBuilder("POST", "/orders")
            .WithBody(new CreateOrderMessage { Status = "1234567890123456789012345678901234567890123456789012345678901234567890" })
            .Build();

        await TestFunctionHosting.SendHttpContextAsync(httpContext);

        var orders = GetPersistedOrders();

        Assert.Empty(orders);

        Assert.Equal(422, httpContext.Response.StatusCode);
    }

    [Fact]
    public void CreateOrder_ThreadSafety()
    {
        var threads = Enumerable.Range(0, 10)
            .Select(_ => new Thread(() => TestFunctionHosting.SendHttpContextAsync(
                new HttpContextBuilder("POST", "/orders")
                    .WithBody(CreateCreateOrderMessage())
                    .Build()).Wait())).ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join(1000);
        }

        var orders = GetPersistedOrders();

        Assert.Equal(10, orders.Length);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(Defaults.Order.Status, orders[i].Status);
            Assert.Equal(Defaults.Order.Name, orders[i].Name);
        }
    }
}