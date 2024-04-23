using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("PUT", "/orders/{id}")]
[Message(MessageTopicNames.OrderUpdate)]
public class UpdateOrderMessageHandler : IMessageHandler<UpdateOrderMessage, OrderDto>
{
    private readonly IOrderService _orderService;

    public UpdateOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IServiceResult<OrderDto>> HandleAsync(UpdateOrderMessage request)
    {
        return await _orderService.UpdateAsync(request);
    }
}