using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;

namespace Benzene.Examples.App.Services;

public class OrderService : IOrderService
{
    private readonly IOrderDbClient _orderDbClient;

    public OrderService(IOrderDbClient orderDbClient)
    {
        _orderDbClient = orderDbClient;
    }

    public async Task<IHandlerResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        var databaseResult = await _orderDbClient.GetAllAsync(pagination.AsPagination());
        return databaseResult.AsHandlerResult();
    }

    public Task<IHandlerResult<OrderDto>> GetAsync(Guid id)
    {
        return _orderDbClient.GetAsync(id).AsHandlerResult();
    }

    public async Task<IHandlerResult<OrderDto>> SaveAsync(CreateOrderMessage value)
    {
        var orderDto = new OrderDto
        {
            Id = Guid.NewGuid(),
            Status = value.Status,
            Name = value.Name
        };

        var databaseResult = await _orderDbClient.CreateAsync(orderDto);

        if (databaseResult.Status == ClientResultStatus.Created)
        {
            return HandlerResult.Created(orderDto);
        }

        return databaseResult.AsHandlerResult<OrderDto>();
    }

    public async Task<IHandlerResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        var result = await _orderDbClient.GetAsync(Guid.Parse(updateOrderMessage.Id));

        if (result.Status == ClientResultStatus.Success)
        {
            var orderDto = result.Payload;

            orderDto.Status = updateOrderMessage.Status;
            orderDto.Name = updateOrderMessage.Name;

            var resultData = await _orderDbClient.UpdateAsync(orderDto);

            return resultData.AsHandlerResult();
        }

        if (result.Status == ClientResultStatus.NotFound)
        {
            return HandlerResult.NotFound<OrderDto>();
        }

        return HandlerResult.ServiceUnavailable<OrderDto>();
    }

    public async Task<IHandlerResult<Guid>> DeleteAsync(Guid id)
    {
        var result = await _orderDbClient.DeleteAsync(id);
        return result.AsHandlerResult();
    }
}