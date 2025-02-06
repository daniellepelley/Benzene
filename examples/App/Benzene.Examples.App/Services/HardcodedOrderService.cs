using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Examples.App.Data.Pagination;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Results;

namespace Benzene.Examples.App.Services;

public class HardcodedOrderService : IOrderService
{
    public Task<IBenzeneResult<OrderDto[]>> GetAllAsync(PaginationMessage pagination)
    {
        return BenzeneResult.Ok(new []
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

    public Task<IBenzeneResult<OrderDto>> GetAsync(Guid id)
    {
        return BenzeneResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IBenzeneResult<OrderDto>> SaveAsync(CreateOrderMessage value)
    {
        return BenzeneResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IBenzeneResult<OrderDto>> UpdateAsync(UpdateOrderMessage updateOrderMessage)
    {
        return BenzeneResult.Ok(new 
            OrderDto
            {
                Id = Guid.NewGuid(),
                Name = "some-name",
                Status = "some-status"
            }
        ).AsTask();
    }

    public Task<IBenzeneResult<Guid>> DeleteAsync(Guid id)
    {
        return BenzeneResult.Ok(Guid.NewGuid()).AsTask();
    }
}