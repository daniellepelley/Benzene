using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
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

    public async Task<IServiceResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        var databaseResult = await _orderDbClient.GetAllAsync(pagination.AsPagination());
        return databaseResult.AsServiceResult();
    }

    public Task<IServiceResult<OrderDto>> GetAsync(Guid id)
    {
        return _orderDbClient.GetAsync(id).AsServiceResult();
    }

    public async Task<IServiceResult<OrderDto>> SaveAsync(CreateOrderMessage value)
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
            return ServiceResult.Created(orderDto);
        }

        return databaseResult.AsServiceResult<OrderDto>();
    }

    public async Task<IServiceResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        var result = await _orderDbClient.GetAsync(Guid.Parse(updateOrderMessage.Id));

        if (result.Status == ClientResultStatus.Ok)
        {
            var orderDto = result.Payload;

            orderDto.Status = updateOrderMessage.Status;
            orderDto.Name = updateOrderMessage.Name;

            var resultData = await _orderDbClient.UpdateAsync(orderDto);

            return resultData.AsServiceResult();
        }

        if (result.Status == ClientResultStatus.NotFound)
        {
            return ServiceResult.NotFound<OrderDto>();
        }

        return ServiceResult.ServiceUnavailable<OrderDto>();
    }

    public async Task<IServiceResult<Guid>> DeleteAsync(Guid id)
    {
        var result = await _orderDbClient.DeleteAsync(id);
        return result.AsServiceResult();
    }
}