using System;
using System.Threading.Tasks;
using Benzene.Core.Results;
using Benzene.Diagnostics.Timers;
using Benzene.Examples.App.Model;

namespace Benzene.Examples.App.Data;

public class ProcessTimeOrderDbClientDecorator : IOrderDbClient
{
    private readonly IOrderDbClient _inner;
    private readonly IProcessTimerFactory _processTimerFactory;

    public ProcessTimeOrderDbClientDecorator(IOrderDbClient inner, IProcessTimerFactory processTimerFactory)
    {
        _inner = inner;
        _processTimerFactory = processTimerFactory;
    }

    public async Task<IClientResult<OrderDto[]>> GetAllAsync(Pagination.Pagination pagination)
    {
        using (_processTimerFactory.Create("Database: Get All Orders"))
        {
            return await _inner.GetAllAsync(pagination);
        }
    }

    public async Task<IClientResult> CreateAsync(OrderDto orderDto)
    {
        using (_processTimerFactory.Create("Database: Create Order"))
        {
            return await _inner.CreateAsync(orderDto);
        }
    }

    public async Task<IClientResult<OrderDto>> GetAsync(Guid id)
    {
        using (_processTimerFactory.Create("Database: Get Order"))
        {
            return await _inner.GetAsync(id);
        }
    }

    public async Task<IClientResult<OrderDto>> UpdateAsync(OrderDto orderDto)
    {
        using (_processTimerFactory.Create("Database: Update Order"))
        {
            return await _inner.UpdateAsync(orderDto);
        }
    }

    public async Task<IClientResult<Guid>> DeleteAsync(Guid id)
    {
        using (_processTimerFactory.Create("Database: Delete Order"))
        {
            return await _inner.DeleteAsync(id);
        }
    }
}