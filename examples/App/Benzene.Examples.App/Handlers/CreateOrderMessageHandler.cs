using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandling;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("POST", "/orders")]
[Message(MessageTopicNames.OrderCreate)]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage, OrderDto>
{
    private readonly IOrderService _orderService;

    public CreateOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IServiceResult<OrderDto>> HandleAsync(CreateOrderMessage request)
    {
        return await _orderService.SaveAsync(request);
    }
}

