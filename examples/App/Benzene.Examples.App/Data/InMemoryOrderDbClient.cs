using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Results;
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

    public Task<IBenzeneResult<OrderDto>> GetAsync(Guid id)
    {
        try
        {
            var order = Orders.FirstOrDefault(x => x.Id == id);
            return order == null
                ? BenzeneResult.NotFound<OrderDto>().AsTask()
                : BenzeneResult.Ok(order.AsOrderDto()).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return BenzeneResult.ServiceUnavailable<OrderDto>().AsTask();
        }
    }

    public Task<IBenzeneResult<OrderDto>> UpdateAsync(OrderDto orderDto)
    {
        try
        {
            var existingOrder = Orders.FirstOrDefault(x => x.Id == orderDto.Id);

            if (existingOrder == null)
            {
                return BenzeneResult.NotFound<OrderDto>().AsTask();
            }

            existingOrder.Status = orderDto.Status;
            existingOrder.Name = orderDto.Name;
            return BenzeneResult.Updated(orderDto).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return BenzeneResult.ServiceUnavailable<OrderDto>().AsTask();

        }
    }

    public Task<IBenzeneResult<Guid>> DeleteAsync(Guid id)
    {
        try
        {
            var order = Orders.FirstOrDefault(x => x.Id == id);


            if (order == null)
            {
                return BenzeneResult.NotFound<Guid>($"Order {id} not found").AsTask();
            }

            Orders.Remove(order);
            return BenzeneResult.Deleted(id).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return BenzeneResult.ServiceUnavailable<Guid>().AsTask();
        }
    }

    public Task<IBenzeneResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination)
    {
        try
        {
            var orderDtos = Orders
                .AsQueryable()
                .Paginate(x => x.Id, pagination)
                .Select(x =>x.AsOrderDto())
                .ToArray();
                
            return BenzeneResult.Ok(orderDtos).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return BenzeneResult.ServiceUnavailable<OrderDto[]>().AsTask();
        }
    }

    public Task<IBenzeneResult> CreateAsync(OrderDto orderDto)
    {
        try
        {
            Orders.Add(orderDto.AsOrder());
            return BenzeneResult.Created().AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error");
            return BenzeneResult.ServiceUnavailable().AsTask();
        }
    }
}