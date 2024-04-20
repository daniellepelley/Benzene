using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Core.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Microsoft.Extensions.Logging;

namespace Benzene.Examples.App.Data;

public class InMemoryOrderDbClient : IOrderDbClient
{
    private readonly ILogger _logger;

    public InMemoryOrderDbClient(ILogger<InMemoryOrderDbClient> logger)
    {
        _logger = logger;
    }

    public static ConcurrentBag<Order> Orders { get; } = new();

    public Task<IClientResult<OrderDto>> GetAsync(Guid id)
    {
        try
        {
            var order = Orders.FirstOrDefault(x => x.Id == id);
            return order == null
                ? ClientResult.NotFound<OrderDto>().AsTask()
                : ClientResult.Ok(order.AsOrderDto()).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return ClientResult.ServiceUnavailable<OrderDto>().AsTask();
        }
    }

    public Task<IClientResult<OrderDto>> UpdateAsync(OrderDto orderDto)
    {
        try
        {
            var existingOrder = Orders.FirstOrDefault(x => x.Id == orderDto.Id);

            if (existingOrder == null)
            {
                return ClientResult.NotFound<OrderDto>().AsTask();
            }

            existingOrder.Status = orderDto.Status;
            existingOrder.Name = orderDto.Name;
            return ClientResult.Updated(orderDto).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return ClientResult.ServiceUnavailable<OrderDto>().AsTask();

        }
    }

    public Task<IClientResult<Guid>> DeleteAsync(Guid id)
    {
        try
        {
            var order = Orders.FirstOrDefault(x => x.Id == id);


            if (order == null)
            {
                return ClientResult.NotFound<Guid>($"Order {id} not found").AsTask();
            }

            Orders.Remove(order);
            return ClientResult.Deleted(id).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return ClientResult.ServiceUnavailable<Guid>().AsTask();
        }
    }

    public Task<IClientResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination)
    {
        try
        {
            var orderDtos = Orders
                .AsQueryable()
                .Paginate(x => x.Id, pagination)
                .Select(x =>x.AsOrderDto())
                .ToArray();
                
            return ClientResult.Ok(orderDtos).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return ClientResult.ServiceUnavailable<OrderDto[]>().AsTask();
        }
    }

    public Task<IClientResult> CreateAsync(OrderDto orderDto)
    {
        try
        {
            Orders.Add(orderDto.AsOrder());
            return ClientResult.Created().AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return ClientResult.ServiceUnavailable().AsTask();
        }
    }
}