using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandling;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("DELETE", "/orders/{id}")]
[Message(MessageTopicNames.OrderDelete)]
public class DeleteOrderMessageHandler : IMessageHandler<DeleteOrderMessage, Guid>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public DeleteOrderMessageHandler(IOrderService orderService, ILogger logger)
    {
        _logger = logger;

        _orderService = orderService;
    }

    public async Task<IHandlerResult<Guid>> HandleAsync(DeleteOrderMessage request)
    {
        _logger.LogInformation("Deleting client");
        return await _orderService.DeleteAsync(Guid.Parse(request.Id));
    }
}