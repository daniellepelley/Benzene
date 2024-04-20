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

[HttpEndpoint("PUT", "/orders/{id}")]
[Message(MessageTopicNames.OrderUpdate)]
public class UpdateOrderMessageHandler : IMessageHandler<UpdateOrderMessage, OrderDto>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public UpdateOrderMessageHandler(IOrderService orderService, ILogger logger)
    {
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<IHandlerResult<OrderDto>> HandleAsync(UpdateOrderMessage request)
    {
        _logger.LogInformation("Updating order");
        return await _orderService.UpdateAsync(request);
    }
}