using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Results;

namespace Benzene.Examples.App.Services;

public class OrderService : IOrderService
{
    private readonly IOrderDbClient _orderDbClient;

    public OrderService(IOrderDbClient orderDbClient)
    {
        _orderDbClient = orderDbClient;
    }

    public async Task<IBenzeneResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        var databaseResult = await _orderDbClient.GetAllAsync(pagination.AsPagination());
        return databaseResult;
    }

    public Task<IBenzeneResult<OrderDto>> GetAsync(Guid id)
    {
        return _orderDbClient.GetAsync(id);
    }

    public async Task<IBenzeneResult<OrderDto>> SaveAsync(CreateOrderMessage value)
    {
        var orderDto = new OrderDto
        {
            Id = Guid.NewGuid(),
            Status = value.Status,
            Name = value.Name
        };

        var databaseResult = await _orderDbClient.CreateAsync(orderDto);

        if (databaseResult.Status == BenzeneResultStatus.Created)
        {
            return BenzeneResult.Created(orderDto);
        }

        return databaseResult.As<OrderDto>();
    }

    public async Task<IBenzeneResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        var result = await _orderDbClient.GetAsync(Guid.Parse(updateOrderMessage.Id));

        if (result.Status == BenzeneResultStatus.Ok)
        {
            var orderDto = result.Payload;

            orderDto.Status = updateOrderMessage.Status;
            orderDto.Name = updateOrderMessage.Name;

            var resultData = await _orderDbClient.UpdateAsync(orderDto);

            return resultData;
        }

        if (result.Status == BenzeneResultStatus.NotFound)
        {
            return BenzeneResult.NotFound<OrderDto>();
        }

        return BenzeneResult.ServiceUnavailable<OrderDto>();
    }

    public async Task<IBenzeneResult<Guid>> DeleteAsync(Guid id)
    {
        var result = await _orderDbClient.DeleteAsync(id);
        return result;
    }
}