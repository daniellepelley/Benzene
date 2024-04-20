using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Google.Tests.Helpers;
using Benzene.Examples.Google.Tests.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Google.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class GetAllOrderTest : InMemoryOrdersTestBase
{
    private const string GetAllOrders = MessageTopicNames.OrderGetAll;
    private readonly Guid _id1 = Guid.Parse(Defaults.Order.Id);
    private readonly Guid _id2 = Guid.Parse(Defaults.Order.Id2);

    private static GetAllOrdersMessage CreateGetAllOrdersMessage()
    {
        return new GetAllOrdersMessage();
    }

    [Fact]
    public async Task GetAllOrders_Http()
    {
        await SetUpDatabaseAsync();
            
        var httpContext = new HttpContextBuilder("GET", "/orders")
            .WithBody(CreateGetAllOrdersMessage())
            .Build();

        await TestFunctionHosting.SendHttpContextAsync(httpContext);

        var orders = httpContext.Response.Body<OrderDto[]>();

        Assert.True(orders.Any(x => x.Id == _id1));
        Assert.True(orders.Any(x => x.Id == _id2));
    }

    private async Task SetUpDatabaseAsync()
    {
        AddOrder(new Order
        {
            Id = _id1,
            Status = Defaults.Order.Status,
            Name = Defaults.Order.Name,
        });
        AddOrder(new Order
        {
            Id = _id2,
            Status = Defaults.Order.Status2,
            Name = Defaults.Order.Name2,
        });
    }
}