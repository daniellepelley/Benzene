using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandling;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("GET", "/orders")]
[Message(MessageTopicNames.OrderGetAll)]
public class GetAllOrderMessageHandler : IMessageHandler<GetAllOrdersMessage, OrderDto[]>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public GetAllOrderMessageHandler(IOrderService orderService, ILogger logger)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<IHandlerResult<OrderDto[]>> HandleAsync(GetAllOrdersMessage request)
    {
        _logger.LogInformation("Getting all orders");
        return await _orderService.GetAllAsync(request.Pagination);
    }
}