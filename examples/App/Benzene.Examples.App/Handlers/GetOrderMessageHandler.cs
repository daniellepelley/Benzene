using System;
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

[HttpEndpoint("GET", "/orders/{id}")]
[Message(MessageTopicNames.OrderGet)]
public class GetOrderMessageHandler : IMessageHandler<GetOrderMessage, OrderDto>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public GetOrderMessageHandler(IOrderService orderService, ILogger<GetAllOrderMessageHandler> logger)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<IHandlerResult<OrderDto>> HandleAsync(GetOrderMessage request)
    {
        _logger.LogInformation("Getting order");
        return await _orderService.GetAsync(Guid.Parse(request.Id));
    }
}