﻿using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.Http;

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

    public async Task<IBenzeneResult<OrderDto>> HandleAsync(UpdateOrderMessage request)
    {
        return await _orderService.UpdateAsync(request);
    }
}