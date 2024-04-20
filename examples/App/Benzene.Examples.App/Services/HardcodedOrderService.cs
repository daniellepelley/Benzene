using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;

namespace Benzene.Examples.App.Services;

public class HardcodedOrderService : IOrderService
{
    public Task<IHandlerResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        return HandlerResult.Ok(new []
        {
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            },
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        }).AsTask();
    }

    public Task<IHandlerResult<OrderDto>> GetAsync(Guid id)
    {
        return HandlerResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IHandlerResult<OrderDto>> SaveAsync(CreateOrderMessage value)
    {
        return HandlerResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IHandlerResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        return HandlerResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IHandlerResult<Guid>> DeleteAsync(Guid id)
    {
        return HandlerResult.Ok(Guid.NewGuid()).AsTask();
    }
}