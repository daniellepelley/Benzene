using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("GET", "/orders")]
[Message(MessageTopicNames.OrderGetAll)]
public class GetAllOrderMessageHandler : IMessageHandler<GetAllOrdersMessage, OrderDto[]>
{
    private readonly IOrderService _orderService;

    public GetAllOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IBenzeneResult<OrderDto[]>> HandleAsync(GetAllOrdersMessage request)
    {
        return await _orderService.GetAllAsync(request?.Pagination);
    }
}