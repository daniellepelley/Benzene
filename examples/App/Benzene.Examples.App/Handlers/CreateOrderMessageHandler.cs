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

[HttpEndpoint("POST", "/orders")]
[Message(MessageTopicNames.OrderCreate)]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage, OrderDto>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public CreateOrderMessageHandler(IOrderService orderService, ILogger logger)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<IHandlerResult<OrderDto>> HandleAsync(CreateOrderMessage request)
    {
        _logger.LogInformation("Creating order");
        return await _orderService.SaveAsync(request);
    }
}