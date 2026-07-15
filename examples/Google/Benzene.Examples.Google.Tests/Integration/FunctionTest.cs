using System.Threading.Tasks;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Google.Tests.Helpers;
using Benzene.Examples.Google.Tests.Helpers.Builders;
using Benzene.GoogleCloud.Functions.Http.TestHelpers;
using Xunit;

namespace Benzene.Examples.Google.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class FunctionTest : InMemoryOrdersTestBase
{
    [Fact]
    public async Task Function_HandleAsync_DispatchesThroughTheRealProductionConstructor()
    {
        var function = new Function();

        var httpContext = new HttpContextBuilder("POST", "/orders")
            .WithBody(new CreateOrderMessage { Status = Defaults.Order.Status, Name = Defaults.Order.Name })
            .Build();

        await function.HandleAsync(httpContext);

        Assert.Equal(201, httpContext.Response.StatusCode);
        var order = httpContext.Response.Body<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(Defaults.Order.Status, order.Status);
    }
}
