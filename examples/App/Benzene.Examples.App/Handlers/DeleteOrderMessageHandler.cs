using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Examples.App.Handlers;

[HttpEndpoint("DELETE", "/orders/{id}")]
[Message(MessageTopicNames.OrderDelete)]
public class DeleteOrderMessageHandler : IMessageHandler<DeleteOrderMessage, Guid>
{
    private readonly IOrderService _orderService;

    public DeleteOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IBenzeneResult<Guid>> HandleAsync(DeleteOrderMessage request)
    {
        return await _orderService.DeleteAsync(Guid.Parse(request.Id));
    }
}