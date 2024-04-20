using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Google.Tests.Helpers;
using Benzene.Examples.Google.Tests.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Google.Tests.Integration
{
    [Trait("Category", "Integration")]
    [Collection("Sequential")]
    public class GetOrderTest : InMemoryOrdersTestBase
    {
        private CreateOrderMessage CreateCreateOrderMessage()
        {
            return new CreateOrderMessage
            {
                Name = Defaults.Order.Name,
                Status = Defaults.Order.Status,
            };
        }

        [Fact]
        public async Task GetOrder_Http()
        {
            var httpContext = new HttpContextBuilder("POST", "/orders")
                .WithBody(CreateCreateOrderMessage())
                .Build();

            await TestFunctionHosting.SendHttpContextAsync(httpContext);
            var createdOrder = GetPersistedOrders().First();

            var getHttpContext = new HttpContextBuilder("GET", $"/orders/{createdOrder.Id}")
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(getHttpContext);
            var response = getHttpContext.Response;
           
            Assert.Equal(200, response.StatusCode);

            var order = response.Body<OrderDto>();
            Assert.NotNull(order);
            Assert.Equal(Defaults.Order.Status, order.Status);
            Assert.Equal(Defaults.Order.Name, order.Name);
        }

        [Fact]
        public async Task GetOrder_Http_WhenDoesNotExist()
        {
            var orderId = Guid.NewGuid();
        
            var deleteHttpContext = new HttpContextBuilder("GET", $"/orders/{orderId}")
                .WithBody(CreateCreateOrderMessage())
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(deleteHttpContext);
            var response = deleteHttpContext.Response;
           
            Assert.Equal(404, response.StatusCode);

            var orders = GetPersistedOrders().ToArray();
            Assert.Empty(orders);
        }

        [Fact]
        public async Task GetOrder_Http_ValidationFailure()
        {
            var orderId = "invalid";
        
            var deleteHttpContext = new HttpContextBuilder("GET", $"/orders/{orderId}")
                .WithBody(CreateCreateOrderMessage())
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(deleteHttpContext);
            var response = deleteHttpContext.Response;
           
            Assert.Equal(422, response.StatusCode);
        }
    }
}