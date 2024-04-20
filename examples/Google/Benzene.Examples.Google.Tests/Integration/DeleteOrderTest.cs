using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Google.Tests.Helpers;
using Benzene.Examples.Google.Tests.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Google.Tests.Integration
{
    [Trait("Category", "Integration")]
    [Collection("Sequential")]
    public class DeleteOrderTest : InMemoryOrdersTestBase
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
        public async Task DeleteOrder_Http()
        {
            var httpContext = new HttpContextBuilder("POST", "/orders")
                .WithBody(CreateCreateOrderMessage())
                .Build();

            await TestFunctionHosting.SendHttpContextAsync(httpContext);

            var order = GetPersistedOrders().First();
            Assert.NotNull(order);

            var deleteHttpContext = new HttpContextBuilder("DELETE", $"/orders/{order.Id}")
                .WithBody(CreateCreateOrderMessage())
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(deleteHttpContext);
            var response = deleteHttpContext.Response;
           
            Assert.Equal(204, response.StatusCode);

            var orders = GetPersistedOrders().ToArray();

            Assert.Empty(orders);
        }

        [Fact]
        public async Task DeleteOrder_Http_WhenDoesNotExist()
        {
            var orderId = Guid.NewGuid();
        
            var deleteHttpContext = new HttpContextBuilder("DELETE", $"/orders/{orderId}")
                .WithBody(CreateCreateOrderMessage())
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(deleteHttpContext);
            var response = deleteHttpContext.Response;
           
            Assert.Equal(404, response.StatusCode);

            var orders = GetPersistedOrders().ToArray();
            Assert.Empty(orders);
        }

        [Fact]
        public async Task DeleteOrder_Http_ValidationFailure()
        {
            var orderId = "invalid";
        
            var deleteHttpContext = new HttpContextBuilder("DELETE", $"/orders/{orderId}")
                .WithBody(CreateCreateOrderMessage())
                .Build();
            
            await TestFunctionHosting.SendHttpContextAsync(deleteHttpContext);
            var response = deleteHttpContext.Response;
           
            Assert.Equal(422, response.StatusCode);
        }
    }
}